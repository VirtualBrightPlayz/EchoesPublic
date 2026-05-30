using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerConsole : Control, ICommandSender
{
    public static PlayerConsole Instance;

    [Export]
    public LineEdit ConsoleInput;

    [Export]
    public RichTextLabel ConsoleOutput;

    [Export]
    public NetworkPlayer Player;

    public bool ConsoleOpen;

    [Signal]
    public delegate void OnCommandResponseEventHandler(string text, bool failed);

    [Export]
    public bool IsAdmin = false;

    public override void _Ready()
    {
        if (IsMultiplayerAuthority())
            Instance = this;

        IsAdmin = GetMultiplayerAuthority() == 1;

        if (ConsoleInput != null)
        {
            ConsoleInput.Visible = false;
            // ConsoleInput.TextSubmitted += ConsoleSubmitted;
        }
        ConsoleInput = null;
        ConsoleOutput = null;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcPassword(string pass)
    {
        IsAdmin = GetMultiplayerAuthority() == 1;
        if (IsAdmin)
            return;
        if (GetMultiplayerAuthority() != Multiplayer.GetRemoteSenderId())
            return;
        if (string.IsNullOrEmpty(Settings.Server.AdminPassword))
            return;
        IsAdmin = pass.Equals(Settings.Server.AdminPassword);
        Log.PrintInfo($"Player {Player.username} ({GetMultiplayerAuthority()}) IsAdmin={IsAdmin}");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcConsoleCommand(string text)
    {
        if (GetMultiplayerAuthority() != Multiplayer.GetRemoteSenderId())
            return;
        if (!IsAdmin)
            return;
        Log.PrintInfo($"Player {Player.username} ({GetMultiplayerAuthority()}) ConsoleCommand={text}");
        ConsoleSubmitted(text);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcConsoleResponse(string text, bool failed)
    {
        if (Multiplayer.GetRemoteSenderId() == 1 && IsMultiplayerAuthority())
        {
            ConsoleResponse(text, failed);
        }
    }

    public async void ConsoleResponse(string text, bool failed)
    {
        if (Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
        {
            RpcId(GetMultiplayerAuthority(), nameof(RpcConsoleResponse), text, failed);
        }
        if (ConsoleOutput == null)
        {
            EmitSignal(SignalName.OnCommandResponse, text, failed);
            if (failed)
                Log.PrintErr(text);
            else
                Log.PrintInfo(text);
            return;
        }
        ConsoleOutput.Text = text;
        ConsoleOutput.Modulate = failed ? new Color(1, 0, 0) : new Color(1, 1, 1);
        await ToSignal(GetTree().CreateTimer(5f), SceneTreeTimer.SignalName.Timeout);
        ConsoleOutput.Text = "";
    }

    public void ConsoleSubmitted(string text)
    {
        Log.Print($"Command ran: {text}");

        if (text.StartsWith('/') && IsMultiplayerAuthority())
        {
            RpcId(1, nameof(RpcConsoleCommand), text.Substring(1));

            ConsoleInput?.Clear();
            // ConsoleResponse("Command sent.", false);
            return;
        }

        if (text.StartsWith('#') && IsMultiplayerAuthority())
        {
            RpcId(1, nameof(RpcPassword), text.Substring(1));

            ConsoleInput?.Clear();
            ConsoleResponse("Password sent.", false);
            return;
        }

        var parts = text.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var cmdName = parts[0].ToLower();
        parts.RemoveAt(0);

        var matchingCommands =
            GameConsole.Instance.RegisteredCommands.Where(x =>
                    x.Command.ToLower() == cmdName ||
                    x.Alias.Any(alias => alias.ToLower() == cmdName)
                ).ToArray();

        if (matchingCommands.Any())
        {
            bool successful = matchingCommands.First().Execute(this, parts.ToArray(), out string response);

            ConsoleResponse(response, !successful);
        }
        else
        {
            ConsoleResponse($"Invalid command: {cmdName}", true);
        }

        ConsoleInput?.Clear();
    }

}
