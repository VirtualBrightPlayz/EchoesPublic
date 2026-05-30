using System;
using System.Collections.Generic;
using Godot;

public partial class GameConsoleInput : LineEdit
{
    public List<string> prevCommands = new List<string>();
    public int prevCmdIndex = -1;

    public void ClearHistory()
    {
        prevCommands.Clear();
        prevCmdIndex = -1;
    }

    public override void _Ready()
    {
        TextSubmitted += Submit;
    }

    public override void _ExitTree()
    {
        TextSubmitted -= Submit;
    }

    private void Submit(string newText)
    {
        prevCommands.Add(newText);
        prevCmdIndex = prevCommands.Count;
    }

    public override void _GuiInput(InputEvent ev)
    {
        if (ev is InputEventKey key)
        {
            if (key.IsActionPressed("ui_up"))
            {
                prevCmdIndex = Mathf.Clamp(prevCmdIndex - 1, 0, prevCommands.Count);
                if (prevCmdIndex >= prevCommands.Count)
                    Text = string.Empty;
                else
                    Text = prevCommands[prevCmdIndex];
                CaretColumn = Text.Length;
                AcceptEvent();
            }
            if (key.IsActionPressed("ui_down"))
            {
                prevCmdIndex = Mathf.Clamp(prevCmdIndex + 1, 0, prevCommands.Count);
                if (prevCmdIndex >= prevCommands.Count)
                    Text = string.Empty;
                else
                    Text = prevCommands[prevCmdIndex];
                CaretColumn = Text.Length;
                AcceptEvent();
            }
        }
    }
}
