// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;

namespace Netick.GodotEngine;

[ExecutionOrder(-1000)]
[IgnoreCodeGen]
[GlobalClass]
public unsafe partial class NetworkTransform2D : NetworkBehaviour<Node2D>
{
    [Export]
    public Node2D TransformSource;
    [Export]
    public Node2D RenderTransform;
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
    //[Export]
    //protected TransformSpace              TransformSpace                 = TransformSpace.Local;

    protected bool _syncPosition;
    protected bool _syncRot;

    protected float _posPrecision;
    protected float _posInversePrecision;
    private float _rotPrecision;
    private float _rotInversePrecision;

    public override void NetworkAwake()
    {
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

        if (TransformSource == null)
        {
            NetickLogger.LogError(Object.Entity, $"{nameof(NetworkTransform2D)}: you must assign a {nameof(Node2D)} to TransformSource on [{this.GetPath()}]\nNote: usually TransformSource should be the same as the TransformSource of NetworkObject.");
            return;
        }

        NetickGodotUtils.SetVector2(S, TransformSource.GlobalPosition, _posInversePrecision);
        NetickGodotUtils.SetFloat(S + 2, TransformSource.GlobalRotation, _rotInversePrecision);


        if (Engine.IsServer)
        {
            for (int i = 0; i < (2 + 1); i++)
                Entity.Dirtify(S + i);
        }
    }

    public unsafe override void NetworkFixedUpdate()
    {
        if (RenderTransform != null && InterpolationSource == InterpolationMode.PredicatedSnapshot /*&& NetTransform.Interpolator.IsLocalInterpData*/)
        {
            RenderTransform.GlobalPosition = TransformSource.GlobalPosition;
            RenderTransform.GlobalRotation = TransformSource.GlobalRotation;
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
            RenderTransform.GlobalPosition = (NetickGodotUtils.GetVector2(stateFromPtr, _posPrecision).Lerp(NetickGodotUtils.GetVector2(stateToPtr, _posPrecision), alpha));
        if (_syncRot)
            RenderTransform.GlobalRotation = Mathf.Lerp(NetickGodotUtils.GetFloat(stateFromPtr + 2, _rotPrecision), NetickGodotUtils.GetFloat(stateToPtr + 2, _rotPrecision), alpha);
    }

    public override void NetcodeIntoGameEngine()
    {
        if (_syncPosition)
            TransformSource.GlobalPosition = NetickGodotUtils.GetVector2(S, _posPrecision);
        if (_syncRot)
            TransformSource.GlobalRotation = NetickGodotUtils.GetFloat(S + 2, _posPrecision);
    }

    public override void GameEngineIntoNetcode()
    {
        //GD.Print("Transform GameEngineIntoNetcode ");

        var oldPos = NetickGodotUtils.GetVector2(S, _posPrecision);  //var newPos = _trans.position;
        var oldRot = NetickGodotUtils.GetFloat(S + 2, _rotPrecision);   //var newRot = _trans.rotation;

        // new poses
        if (_syncPosition)
            NetickGodotUtils.SetVector2(S, TransformSource.GlobalPosition, _posInversePrecision);
        if (_syncRot)
            NetickGodotUtils.SetFloat(S + 2, TransformSource.GlobalRotation, _rotInversePrecision);

        var newPos = NetickGodotUtils.GetVector2(S, _posPrecision);
        var newRot = NetickGodotUtils.GetFloat(S + 2, _rotPrecision);

        if (Engine.IsServer /*&& EnableNetworking*/)
        {
            //Logger.Log("DIRTY");
            if (_syncPosition)
            {
                if (oldPos.X != newPos.X)
                    Entity.Dirtify(S + 0);
                if (oldPos.Y != newPos.Y)
                    Entity.Dirtify(S + 1);
            }

            if (_syncRot)
            {
                if (oldRot != newRot)
                    Entity.Dirtify(S + 2);
            }

            Engine.Grid.Move(Entity, new NetickVector3(oldPos.X, oldPos.Y, 0), new NetickVector3(newPos.X, newPos.Y, 0));
        }

        Entity.Pos = new NetickVector3(newPos.X, newPos.Y, 0);
    }

    public override int InternalGetStateSizeWords()
    {
        return (2) + (1) + 1;
    }

}

