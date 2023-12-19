using Godot;

namespace Netick.GodotEngine;

public abstract partial class NetworkBehaviour : BaseNetworkBehaviour
{
    protected T GetBaseNode<T>() where T : Node
    {
        var parent = GetParent();

        if (parent == null)
        {
            GD.Print("NetworkBehaviour cannot have no parent.");
            QueueFree();
            return null;
        }

        if (parent is not T typedParent)
        {
            GD.Print("NetworkBehaviour of type " + typeof(T).Name + " cannot be attached to parent of type " + parent.GetType().Name);
            QueueFree();
            return null;
        }

        return typedParent;
    }
}
