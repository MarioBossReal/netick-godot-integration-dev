using Godot;
using Netick.GodotEngine;
using System;

namespace Netick.Samples.Bomberman
{
  [GlobalClass]
  public partial class eventsTest : NetworkEventsListener
  {

    public override void OnStartup(NetworkSandbox sandbox)
    {
      GD.Print("WORKS FINE!");
    }

  }

}
