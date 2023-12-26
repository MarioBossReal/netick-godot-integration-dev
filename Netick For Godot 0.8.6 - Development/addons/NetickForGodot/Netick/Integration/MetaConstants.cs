using Godot;

namespace Netick.GodotEngine.Constants;
public static class MetaConstants
{
    /// <summary>
    /// The metadata used to tag a <see cref="Node"/> as networked. The value of this metadata is unused.
    /// </summary>
    public static readonly StringName NetworkedNode = "networked_node";

    /// <summary>
    /// The metadata used to tag a <see cref="Node"/> as the child of a networked node.
    /// The value of this metadata is an <see cref="int"/> corresponding to the <see cref="Node.Owner"/>'s prefab id.
    /// </summary>
    public static readonly StringName OwnerPrefabId = "owner_prefab_id";

    /// <summary>
    /// The metadata used to supply a networked <see cref="Node"/> with a reference to the <see cref="Netick.GodotEngine.NetworkObject"/> which wraps it, should it have one.
    /// The value of this metadata is a <see cref="Netick.GodotEngine.NetworkObject"/>.
    /// </summary>
    public static readonly StringName NetworkObject = "network_object";
}
