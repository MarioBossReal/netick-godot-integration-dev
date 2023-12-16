using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections;
using System.Runtime.CompilerServices;
using Netick;

namespace Netick.GodotEngine
{

  public class NetworkArray : INetworkArray
  {
    public int                    Length => _length;

    internal NetworkArrayMeta     _flags;
    internal unsafe int*          _intS;
    internal int                  _length;
    internal int                  _elementSizeWords;
    internal INetickNetworkScript _beh;

    public unsafe virtual void InternalInit(INetickNetworkScript beh, int* state, int elementSizeWords, int flags)
    {
      _beh               = beh;
      _elementSizeWords  = elementSizeWords;
      _intS              = state;

      _flags = (NetworkArrayMeta)flags;
    }
 
    public virtual void     InternalReset() { }

    internal virtual string PrintValue(int index) { return "PrintValue"; }
  }

  [Serializable]
  public class NetworkArray<T> : NetworkArray, INetworkArray, IEnumerable<T> where T : unmanaged 
  { 
    private T[]        _array;
    private int        _counter;

    public NetworkArray(int capacity) 
    {
      _length    = capacity;
      _array     = new T[capacity];
    }

    public unsafe override void InternalInit(INetickNetworkScript beh, int* state, int elementSizeWords, int flags)
    {
      base.InternalInit(beh, state, elementSizeWords, flags);
    }

    public IEnumerator<T> GetEnumerator()
    {
      return (IEnumerator<T>)_array.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.GetEnumerator();
    }

    public unsafe void Add(T element)
    {
      _array[_counter] = element;
      _counter++;
    }

    public unsafe override void InternalReset()
    {
      for (int i = 0; i < _length; i++)
      {
        this[i] = _array[i];
      }
    }

    internal override string PrintValue(int index)
    {
      return this[index].ToString();
    }

    public unsafe T this[int i]
    {
      get
      {
        if (_intS == null)
          return _array[i];

        return *(T*)(_intS + (i * _elementSizeWords));
      }

      set
      {
        if (_intS == null)
          _array[i] = value; 
        else
          Entity.InternalDirtify(_beh, (int*)(&value), (int*)(_intS + (i * _elementSizeWords)), _elementSizeWords, 
            ((_flags & NetworkArrayMeta.HasOnChanged) == NetworkArrayMeta.HasOnChanged) ? 1 : 0, 
            ((_flags & NetworkArrayMeta.IsInputSourceOnly) == NetworkArrayMeta.IsInputSourceOnly) ? 1 : 0);
      }
    }
  }
}
