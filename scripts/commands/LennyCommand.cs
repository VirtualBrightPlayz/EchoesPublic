using System;
using Godot;

[HiddenCommand]
public class LennyCommand : SimpleGameCommandBase
{
    public override string Command { get; } = "Lenny";

    public override string[] Alias { get; } = Array.Empty<string>();

    public override string CommandDescription => "Lenny";

    public override bool Execute(string[] args, out string response)
    {
        var c = Color.FromHsv(GD.Randf(), 1f, 1f);
        response = $"[color=#{c.ToHtml()}]( ͡° ͜ʖ ͡°)[/color]";
        return true;
    }
}
