using Godot;
using Netick.GodotEngine.Constants;
using System.Collections.Generic;
using System.Linq;

namespace Netick.GodotEngine.Extensions;
public static class NodeExtensions
{
    /// <summary>
    /// Returns the first child of this <see cref="Node"/> that matches type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    public static T GetChild<T>(this Node node) where T : Node
    {
        var children = node.GetChildren();
        return (T)children.FirstOrDefault(x => x is T);
    }

    /// <summary>
    /// Returns all children of this <see cref="Node"/> that match type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<T> GetChildren<T>(this Node node) where T : Node
    {
        var children = node.GetChildren();
        return children.Where(x => x is T).Cast<T>();
    }

    /// <summary>
    /// Returns the first child of this <see cref="Node"/>'s parent that matches type <typeparamref name="T"/>, excluding itself.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    public static T GetSibling<T>(this Node node) where T : Node
    {
        var parent = node.GetParent();

        if (parent == null)
            return null;

        return GetChildren<T>(parent).Where(x => x != node).FirstOrDefault();
    }

    /// <summary>
    /// Returns all children of this <see cref="Node"/>'s parent that match type <typeparamref name="T"/>, excluding itself.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<T> GetSiblings<T>(this Node node) where T : Node
    {
        var parent = node.GetParent();

        if (parent == null)
            return null;

        return GetChildren<T>(parent).Where(x => x != node);
    }

    /// <summary>
    /// Returns all children of this <see cref="Node"/>'s parent, excluding itself.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<Node> GetSiblings(this Node node)
    {
        return GetSiblings<Node>(node);
    }

    /// <summary>
    /// Returns the first descendant of this <see cref="Node"/> that matches type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    public static T GetDescendant<T>(this Node node) where T : Node
    {
        var descendants = GetDescendantsInternal(node);
        return (T)descendants.FirstOrDefault(x => x is T);
    }

    /// <summary>
    /// Returns all descendants of this <see cref="Node"/> that match type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<T> GetDescendants<T>(this Node node) where T : Node
    {
        var descendants = GetDescendantsInternal(node);
        return descendants.Where(x => x is T).Cast<T>();
    }

    /// <summary>
    /// Returns all descendants of this <see cref="Node"/>.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<Node> GetDescendants(this Node node)
    {
        return GetDescendantsInternal(node);
    }

    private static IEnumerable<Node> GetDescendantsInternal(Node node)
    {
        var descendants = node.GetChildren<Node>();

        foreach (var child in node.GetChildren())
        {
            descendants = descendants.Concat(GetDescendantsInternal(child));
        }

        return descendants;
    }

    /// <summary>
    /// Returns the <see cref="Netick.GodotEngine.NetworkObject"/> that wraps this <see cref="Node"/>, should it have one.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static NetworkObject GetNetworkObject(this Node node)
    {
        if (!node.HasMeta(MetaConstants.NetworkObject))
            return null;

        return node.GetMeta(MetaConstants.NetworkObject).As<NetworkObject>();
    }

    public static bool IsNetworked(this Node node)
    {
        return node.HasMeta(MetaConstants.NetworkedNode);
    }
}
