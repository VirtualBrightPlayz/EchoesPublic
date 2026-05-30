using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class LaunchDebugger : Node
{
	public override void _EnterTree()
	{
		if (OS.HasFeature("launch_debugger"))
		{
			System.Diagnostics.Debugger.Launch();
		}
	}
}
