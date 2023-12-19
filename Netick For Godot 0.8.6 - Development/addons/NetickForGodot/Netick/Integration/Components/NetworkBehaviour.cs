using Godot;

namespace Netick.GodotEngine;

public abstract partial class NetworkBehaviour<T> : BaseNetworkBehaviour where T : Node
{
    public T BaseNode { get; private set; }

    private bool _initialized = false;

    protected void InitializeBaseNode()
    {
        if (_initialized)
            return;

        var parent = GetParent();

        if (parent == null)
        {
            GD.Print("NetworkBehaviour cannot have no parent.");
            QueueFree();
            return;
        }

        if (parent is not T typedParent)
        {
            GD.Print("NetworkBehaviour of type " + typeof(T).Name + " cannot be attached to parent of type " + parent.GetType().Name);
            QueueFree();
            return;
        }

        BaseNode = typedParent;
        _initialized = true;
    }
}
