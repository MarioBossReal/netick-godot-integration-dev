// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;

namespace Netick.GodotEngine
{
    [Tool]
    [GlobalClass]
    public sealed partial class NetworkLevel : Node
    {
        //  [Export]
        internal string ThisScriptName = nameof(NetworkLevel);
        public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
        {
            return new Godot.Collections.Array<Godot.Collections.Dictionary>()
    {

        new Godot.Collections.Dictionary()
        {
            { "name",  nameof(ThisScriptName) },
            { "type",  (int)Variant.Type.String },
            { "usage", (int)(PropertyUsageFlags.ReadOnly) }
        }
    };
        }

        public override Variant _Get(StringName property)
        {
            if (property.ToString() == nameof(ThisScriptName))
            {
                return Variant.From(nameof(NetworkLevel));
            }

            return default;
        }

    }
}
