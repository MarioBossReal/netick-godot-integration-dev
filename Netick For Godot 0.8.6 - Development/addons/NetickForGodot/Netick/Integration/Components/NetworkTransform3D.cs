// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;
using System;

namespace Netick.GodotEngine;

public enum TransformDimension
{
    ThreeDimensional,
    TwoDimensional
};

public enum TransformSpace
{
    Local,
    World
}

[Serializable]
[Flags]
public enum NetworkTransformRepConditions
{
    SyncPosition = 0x1,
    SyncRotation = 0x2,
    CompressPosition = 0x8,
    CompressRotation = 0x10,
}

[ExecutionOrder(-1000)]
[IgnoreCodeGen]
[GlobalClass]
public unsafe partial class NetworkTransform3D : NetworkBehaviour
{
    [Export]
    public Node3D RenderTransform;
    [Export]
    private InterpolationMode InterpolationSource = InterpolationMode.Auto;
    [Export]
    public float TeleportDistance = 50f;
    [Export]
    public float PositionPrecision = 0.001f;
    [Export]
    public float RotationPrecision = 0.001f;
    [Export]
    public NetworkTransformRepConditions Settings = NetworkTransformRepConditions.SyncPosition | NetworkTransformRepConditions.SyncRotation;
    //  [Export]
    //  protected TransformSpace              TransformSpace                 = TransformSpace.Local;

    protected bool _syncPosition;
    protected bool _syncRot;

    protected float _posPrecision;
    protected float _posInversePrecision;
    private float _rotPrecision;
    private float _rotInversePrecision;

    private Node3D _transformSource;

    public override void NetworkAwake()
    {
        _transformSource = GetBaseNode<Node3D>();

        _posPrecision = PositionPrecision;
        _posInversePrecision = 1f / PositionPrecision;
        _rotPrecision = RotationPrecision;
        _rotInversePrecision = 1f / RotationPrecision;

        _syncPosition = (Settings & NetworkTransformRepConditions.SyncPosition) == NetworkTransformRepConditions.SyncPosition;
        _syncRot = (Settings & NetworkTransformRepConditions.SyncRotation) == NetworkTransformRepConditions.SyncRotation;
        bool compressPosition = (Settings & NetworkTransformRepConditions.CompressPosition) == NetworkTransformRepConditions.CompressPosition;
        bool compressRot = (Settings & NetworkTransformRepConditions.CompressRotation) == NetworkTransformRepConditions.CompressRotation;

        if (!compressPosition)
        {
            _posPrecision = -1;
            _posInversePrecision = -1;
        }

        if (!compressRot)
        {
            _rotPrecision = -1;
            _rotInversePrecision = -1;
        }

        if (_transformSource == null)
        {
            NetickLogger.LogError(Object.Entity, $"{nameof(NetworkTransform3D)}: you must assign a {nameof(Node3D)} to TransformSource on [{this.GetPath()}]\nNote: usually TransformSource should be the same as the TransformSource of NetworkObject.");
            return;
        }

        NetickGodotUtils.SetVector3(S, _transformSource.GlobalPosition, _posInversePrecision);
        NetickGodotUtils.SetQuaternion(S + 3, _transformSource.Quaternion, _rotInversePrecision);

        if (Engine.IsServer)
        {
            for (int i = 0; i < (3 + 4); i++)
                Entity.Dirtify(S + i);
        }
    }

    public unsafe override void NetworkFixedUpdate()
    {
        if (RenderTransform != null && InterpolationSource == InterpolationMode.PredicatedSnapshot /*&& NetTransform.Interpolator.IsLocalInterpData*/)
        {
            RenderTransform.GlobalPosition = _transformSource.GlobalPosition;
            RenderTransform.Quaternion = _transformSource.Quaternion;
        }
    }

    public override void NetworkRender()
    {
        // GD.Print("NetworkRender");
        if (RenderTransform != null)
            Interpolate();
    }

    public void Interpolate()
    {
        //Interpolation interpolation = _interpolationSource == InterpolationMode.PredicatedSnapshot ? Object.InternalSandbox._localInterpolation : Object.InternalSandbox._remoteInterpolation;
        bool isServer = IsServer;
        Interpolation interpolation = null;

        if (isServer)
            interpolation = Object.Engine.LocalInterpolation;
        else
        {
            if (InterpolationSource == InterpolationMode.PredicatedSnapshot)
                interpolation = Object.Engine.LocalInterpolation;
            else if (InterpolationSource == InterpolationMode.RemoteSnapshot)
                interpolation = Object.Engine.RemoteInterpolation;
            else
            {
                if (Object.IsInputSource)
                    interpolation = Object.Engine.LocalInterpolation;
                else
                    interpolation = Object.Engine.RemoteInterpolation;
            }
        }


        if (!interpolation.HasSnapshots)
            return;
        //Logger.Log("BUFF COUNT: " + ((RemoteInterpolation)interpolation)._buffer.Count);
        var offsetBytes = Entity.StateOffsetBytes + ((byte*)S - (byte*)Entity.State);
        float alpha = interpolation.Alpha;
        byte* snapFrom = (byte*)interpolation.FromSnapshot.Pools[Entity.PoolIndex].Ptr;
        byte* snapTo = (byte*)interpolation.ToSnapshot.Pools[Entity.PoolIndex].Ptr;
        int* stateFromPtr = (int*)(snapFrom + offsetBytes);
        int* stateToPtr = (int*)(snapTo + offsetBytes);

        if (_syncPosition)
            RenderTransform.GlobalPosition = (NetickGodotUtils.GetVector3(stateFromPtr, _posPrecision).Lerp(NetickGodotUtils.GetVector3(stateToPtr, _posPrecision), alpha));
        if (_syncRot)
            RenderTransform.Quaternion = (NetickGodotUtils.GetQuaternion(stateFromPtr + 3, _rotPrecision).Slerp(NetickGodotUtils.GetQuaternion(stateToPtr + 3, _rotPrecision), alpha));
    }

    public override void NetcodeIntoGameEngine()
    {
        //  GD.Print("NetcodeIntoGameEngine");

        _transformSource.GlobalPosition = NetickGodotUtils.GetVector3(S, _posPrecision);
        _transformSource.Quaternion = NetickGodotUtils.GetQuaternion(S + 3, _rotPrecision);
    }

    public override void GameEngineIntoNetcode()
    {
        //GD.Print("Transform GameEngineIntoNetcode ");

        var oldPos = NetickGodotUtils.GetVector3(S, _posPrecision);  //var newPos = _trans.position;
        var oldRot = NetickGodotUtils.GetQuaternion(S + 3, _rotPrecision);   //var newRot = _trans.rotation;

        var oldPotNum = *(NetickVector3*)&oldPos;
        // var oldQuatNum = *(System.Numerics.Quaternion*)&oldRot;

        // new poses
        if (_syncPosition)
            NetickGodotUtils.SetVector3(S, _transformSource.GlobalPosition, _posInversePrecision);
        if (_syncRot)
            NetickGodotUtils.SetQuaternion(S + 3, _transformSource.Quaternion, _rotInversePrecision);

        var newPos = NetickGodotUtils.GetVector3(S, _posPrecision);
        var newRot = NetickGodotUtils.GetQuaternion(S + 3, _rotPrecision);

        var newPosNum = *(NetickVector3*)&newPos;

        if (Engine.IsServer /*&& EnableNetworking*/)
        {
            //Logger.Log("DIRTY");
            if (_syncPosition)
            {
                if (oldPos.X != newPos.X)
                    Entity.Dirtify(S + 0);
                if (oldPos.Y != newPos.Y)
                {
                    // NetickLogger.Log("dirty");
                    Entity.Dirtify(S + 1);
                }
                if (oldPos.Z != newPos.Z)
                    Entity.Dirtify(S + 2);
            }

            if (_syncRot)
            {
                if (oldRot.X != newRot.X)
                    Entity.Dirtify(S + 3);
                if (oldRot.Y != newRot.Y)
                    Entity.Dirtify(S + 4);
                if (oldRot.Z != newRot.Z)
                    Entity.Dirtify(S + 5);
                if (oldRot.X != newRot.X)
                    Entity.Dirtify(S + 6);
            }

            Engine.Grid.Move(Entity, oldPotNum, newPosNum);
        }

        Entity.Pos = newPosNum;
    }

    public override int InternalGetStateSizeWords()
    {
        return (3) + (4) + 1;
    }

}

