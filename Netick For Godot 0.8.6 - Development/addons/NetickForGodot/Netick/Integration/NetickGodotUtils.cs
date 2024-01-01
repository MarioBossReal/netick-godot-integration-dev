// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections;
using Godot;

namespace Netick.GodotEngine
{
  public sealed class ObjectList : IEnumerable<NetworkObject>
  {
    private readonly Dictionary<int, NetworkObject> _entities;

    internal ObjectList(NetworkSandbox sandbox)
    {
      this._entities = sandbox.Entities;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      foreach (var e in _entities)
      {
        yield return e.Value;
      }
    }


    public IEnumerator<NetworkObject> GetEnumerator()
    {
      foreach (var e in _entities)
      {
        yield return e.Value;
      }
    }
  }

  public unsafe class NetickGodotUtils
  {
    public static T FindObjectOfType<T>(Node parent) where T : Node
    {
      T result = null;

      if (parent is T)
        return parent as T;

      foreach (var child in parent.GetChildren())
      {
        result = FindObjectOfType<T>(child);

        if (result != null)
          return result;
      }
       
      return result;
    }


    public static void FindObjectsOfType<T>(Node root, List<T> results) where T : Node
    {
      foreach (var child in root.GetChildren())
      {
        _FindObjectsOfType(child, results);
      }
    }

    private static void _FindObjectsOfType<T>(Node parent, List<T> objs) where T : Node
    {
      if (parent is T)
        objs.Add(parent as T);

      foreach (var child in parent.GetChildren())    
        _FindObjectsOfType(child, objs);
    }

    public static System.Numerics.Vector3 Vector3EngineToNetick(Vector3 from)
    {
      return new System.Numerics.Vector3(from.X, from.Y, from.Z);
    }

    public static Vector3 Vector3NetickToEngine(System.Numerics.Vector3 from)
    {
      return new Vector3(from.X, from.Y, from.Z);
    }

    public unsafe static float GetFloat(int* data, float precision)
    {
      float result = default;

      if (precision == -1)
        result = *(float*)(data + 0);
      else
        result = (float)data[0] * precision;

      return result;
    }

    public unsafe static void SetFloat(int* data, float value, float precisionInverse)
    {
      if (precisionInverse == -1)
        *(float*)(data + 0) = value;
      else
        data[0] = ((value > 0f) ? ((int)(value * precisionInverse + 0.5f)) : ((int)(value * precisionInverse - 0.5f)));
    }

    public unsafe static void SetColor(int* data, Color value, float precisionInverse)
    {
      if (precisionInverse == -1)
      {
        *(float*)(data + 0) = value.R;
        *(float*)(data + 1) = value.G;
        *(float*)(data + 2) = value.B;
        *(float*)(data + 3) = value.A;
      }
      else
      {
        data[0] = ((value.R > 0f) ? ((int)(value.R * precisionInverse + 0.5f)) : ((int)(value.R * precisionInverse - 0.5f)));
        data[1] = ((value.G > 0f) ? ((int)(value.G * precisionInverse + 0.5f)) : ((int)(value.G * precisionInverse - 0.5f)));
        data[2] = ((value.B > 0f) ? ((int)(value.B * precisionInverse + 0.5f)) : ((int)(value.B * precisionInverse - 0.5f)));
        data[3] = ((value.A > 0f) ? ((int)(value.A * precisionInverse + 0.5f)) : ((int)(value.A * precisionInverse - 0.5f)));
      }
    }

    public unsafe static Color GetColor(int* data, float precision)
    {
      Color result = default(Color);
      if (precision == -1)
      {
        result.R = *(float*)(data + 0);
        result.G = *(float*)(data + 1);
        result.B = *(float*)(data + 2);
        result.A = *(float*)(data + 3);
      }
      else
      {
        result.R = (float)data[0] * precision;
        result.G = (float)data[1] * precision;
        result.B = (float)data[2] * precision;
        result.A = (float)data[3] * precision;
      }
      return result;
    }

    public unsafe static Vector2 GetVector2(int* data, float precision)
    {
      Vector2 result = default(Vector2);
      if (precision == -1)
      {
        result.X = *(float*)(data + 0);
        result.Y = *(float*)(data + 1);
      }
      else
      {
        result.X = (float)data[0] * precision;
        result.Y = (float)data[1] * precision;
      }
      return result;
    }

    public unsafe static void SetVector2(int* data, Vector2 value, float precisionInverse)
    {
      if (precisionInverse == -1)
      {
        *(float*)(data + 0) = value.X;
        *(float*)(data + 1) = value.Y;
      }
      else
      {
        data[0] = ((value.X > 0f) ? ((int)(value.X * precisionInverse + 0.5f)) : ((int)(value.X * precisionInverse - 0.5f)));
        data[1] = ((value.Y > 0f) ? ((int)(value.Y * precisionInverse + 0.5f)) : ((int)(value.Y * precisionInverse - 0.5f)));
      }
    }

    public unsafe static Vector3 GetVector3(int* data, float precision)
    {
      Vector3 result = default(Vector3);
      if (precision == -1)
      {
        result.X = *(float*)(data + 0);
        result.Y = *(float*)(data + 1);
        result.Z = *(float*)(data + 2);
      }
      else
      {
        result.X = (float)data[0] * precision;
        result.Y = (float)data[1] * precision;
        result.Z = (float)data[2] * precision;
      }
      return result;
    }

    public unsafe static void SetVector3(int* data, Vector3 value, float precisionInverse)
    {
      if (precisionInverse == -1)
      {
        *(float*)(data + 0) = value.X;
        *(float*)(data + 1) = value.Y;
        *(float*)(data + 2) = value.Z;
      }
      else
      {
        data[0] = ((value.X > 0f) ? ((int)(value.X * precisionInverse + 0.5f)) : ((int)(value.X * precisionInverse - 0.5f)));
        data[1] = ((value.Y > 0f) ? ((int)(value.Y * precisionInverse + 0.5f)) : ((int)(value.Y * precisionInverse - 0.5f)));
        data[2] = ((value.Z > 0f) ? ((int)(value.Z * precisionInverse + 0.5f)) : ((int)(value.Z * precisionInverse - 0.5f)));
      }
    }

    public unsafe static void SetQuaternion(int* data, Quaternion value, float precisionInverse)
    {
      if (precisionInverse == -1)
      {
        *(float*)(data + 0) = value.X;
        *(float*)(data + 1) = value.Y;
        *(float*)(data + 2) = value.Z;
        *(float*)(data + 3) = value.W;
      }
      else
      {
        data[0] = ((value.X > 0f) ? ((int)(value.X * precisionInverse + 0.5f)) : ((int)(value.X * precisionInverse - 0.5f)));
        data[1] = ((value.Y > 0f) ? ((int)(value.Y * precisionInverse + 0.5f)) : ((int)(value.Y * precisionInverse - 0.5f)));
        data[2] = ((value.Z > 0f) ? ((int)(value.Z * precisionInverse + 0.5f)) : ((int)(value.Z * precisionInverse - 0.5f)));
        data[3] = ((value.W > 0f) ? ((int)(value.W * precisionInverse + 0.5f)) : ((int)(value.W * precisionInverse - 0.5f)));
      }
    }

    public unsafe static Quaternion GetQuaternion(int* data, float precision)
    {
      Quaternion result = default(Quaternion);
      if (precision == -1)
      {
        result.X = *(float*)(data + 0);
        result.Y = *(float*)(data + 1);
        result.Z = *(float*)(data + 2);
        result.W = *(float*)(data + 3);
      }
      else
      {
        result.X = (float)data[0] * precision;
        result.Y = (float)data[1] * precision;
        result.Z = (float)data[2] * precision;
        result.W = (float)data[3] * precision;
      }
      return result;
    }
  }
}
