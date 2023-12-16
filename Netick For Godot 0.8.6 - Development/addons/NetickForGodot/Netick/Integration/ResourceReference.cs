using Godot;

namespace Netick.GodotEngine;

[Tool]
[GlobalClass]
public partial class ResourceReference : Resource
{
    [Export]
    public string Name { get; set; }

    [Export]
    public string Path { get; set; }

    [Export]
    public int Id { get; set; }
}
