using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Netick.GodotEngine.Extensions;
public static class NodeExtensions
{
    public static T GetChild<T>(this Node node) where T : Node
    {
        var children = node.GetChildren();
        return (T)children.FirstOrDefault(x => x is T);
    }

    public static IEnumerable<T> GetChildren<T>(this Node node) where T : Node
    {
        var children = node.GetChildren();
        return (IEnumerable<T>)children.Where(x => x is T);
    }

    public static T GetSibling<T>(this Node node) where T : Node
    {
        var parent = node.GetParent();

        if (parent == null)
            return null;

        return GetChildren<T>(parent).Where(x => x != node).FirstOrDefault();
    }

    public static IEnumerable<T> GetSiblings<T>(this Node node) where T : Node
    {
        var parent = node.GetParent();

        if (parent == null)
            return null;

        return GetChildren<T>(parent).Where(x => x != node);
    }

    public static T GetDescendant<T>(this Node node) where T : Node
    {
        var descendants = GetDescendantsInternal(node);
        return (T)descendants.FirstOrDefault(x => x is T);
    }

    public static IEnumerable<T> GetDescendants<T>(this Node node) where T : Node
    {
        var descendants = GetDescendantsInternal(node);
        return (IEnumerable<T>)descendants.Where(x => x is T);
    }

    private static IEnumerable<Node> GetDescendantsInternal(Node node)
    {
        var descendants = node.GetChildren<Node>();

        foreach (var child in descendants)
        {
            descendants.Concat(GetDescendantsInternal(child));
        }

        return descendants;
    }
}
