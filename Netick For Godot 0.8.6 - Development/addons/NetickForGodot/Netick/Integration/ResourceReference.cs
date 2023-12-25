using Godot;

namespace Netick.GodotEngine;

[Tool]
[GlobalClass]
public partial class ResourceReference : Resource
{
    [Export]
    public StringName Name { get; set; }

    [Export]
    public StringName Path { get; set; }

    [Export]
    public int Id { get; set; }
}
