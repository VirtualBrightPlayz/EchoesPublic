using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Godot;

[GlobalClass]
public partial class GameConsole : Node, ICommandSender
{
    public static GameConsole Instance;

    public List<IConsoleCommand> RegisteredCommands = new List<IConsoleCommand>();

    [Signal]
    public delegate void OnLogSetTextEventHandler(string text);
    [Signal]
    public delegate void OnLogClearedEventHandler();
    [Signal]
    public delegate void OnCommandRunEventHandler();
    [Signal]
    public delegate void OnConsoleOpenedEventHandler();
    [Signal]
    public delegate void OnConsoleClosedEventHandler();

    private string _logText = string.Empty;
    private Godot.Mutex mutex = new Godot.Mutex();
    private List<string> logs = new List<string>();

    [Export]
    public RichTextLabel label;
    [Export]
    public Control consoleRoot;

    public override void _EnterTree()
    {
        Instance = this;
        Log.OnLogPrinted += OnLoggedPre;
        if (IsInstanceValid(consoleRoot))
            consoleRoot.VisibilityChanged += Vis;
        RegisterCommands();
    }

    public override void _ExitTree()
    {
        Log.OnLogPrinted -= OnLoggedPre;
        if (IsInstanceValid(consoleRoot))
            consoleRoot.VisibilityChanged -= Vis;
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed && IsInstanceValid(consoleRoot) && (key.PhysicalKeycode == Key.Quoteleft || (consoleRoot.Visible && key.PhysicalKeycode == Key.Escape)))
        {
            consoleRoot.Visible = !consoleRoot.Visible;
            if (consoleRoot.Visible)
                EmitSignal(SignalName.OnConsoleOpened);
            else
                EmitSignal(SignalName.OnConsoleClosed);
            if (!consoleRoot.Visible && IsInstanceValid(NetworkPlayer.LocalInstance))
            {
                InputManager.Instance.ChangeActionSet("InGame"); // TODO: somehow switch to the last used action set, not the same one each time.
            }
            else
            {
                InputManager.Instance.ChangeActionSet("Menu");
            }
            GetViewport().SetInputAsHandled();
        }
    }

    public void RegisterCommands()
    {
        RegisteredCommands.Clear();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                try
                {
                    if (type.IsAssignableTo(typeof(IConsoleCommand)))
                    {
                        var cmdInst = type.GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object>());
                        if (cmdInst is IConsoleCommand cmd)
                        {
                            RegisteredCommands.Add(cmd);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.PrintErr($"Unable to register command: {e}");
                }
            }
        }
    }

    public void OnCommand(string text)
    {
        if (IsInstanceValid(PlayerConsole.Instance))
        {
            PlayerConsole.Instance.ConsoleSubmitted(text);
        }
        else
        {
            ConsoleSubmitted(text);
        }
        EmitSignal(SignalName.OnCommandRun);
    }

    public void ConsoleSubmitted(string text)
    {
        Log.Print($"Command ran: {text}");

        var parts = text.Split(" ").ToList();
        var cmdName = parts[0].ToLower();
        parts.RemoveAt(0);

        var matchingCommands =
            RegisteredCommands.Where(x =>
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
    }

    public void ConsoleResponse(string text, bool failed)
    {
        if (failed)
            Log.PrintErr(text);
        else
            Log.PrintInfo(text);
    }

    public void ClearLogs()
    {
        mutex.Lock();
        try
        {
            logs.Clear();
        }
        finally
        {
            mutex.Unlock();
        }
        _logText = string.Empty;
        if (IsInstanceValid(label))
            label.Text = _logText;
        EmitSignal(SignalName.OnLogCleared);
    }

    public void ToggleFullscreen()
    {
        SetFullscreen(!Settings.User.ConsoleFullscreen);
    }

    public void SetFullscreen(bool value)
    {
        if (IsInstanceValid(consoleRoot))
        {
            int c = 100;
            if (value)
                c = 0;
            consoleRoot.AddThemeConstantOverride("margin_top", c);
            consoleRoot.AddThemeConstantOverride("margin_left", c);
            consoleRoot.AddThemeConstantOverride("margin_bottom", c);
            consoleRoot.AddThemeConstantOverride("margin_right", c);
        }
        if (Settings.User.ConsoleFullscreen != value)
        {
            Settings.User.ConsoleFullscreen = value;
            Settings.WriteUserSettings();
        }
    }

    private void Vis()
    {
        SetFullscreen(Settings.User.ConsoleFullscreen);
    }

    private void OnLoggedPre(object sender, Log.LogEventArgs args)
    {
        while (!mutex.TryLock())
        {
            if (OS.GetMainThreadId() != OS.GetThreadCallerId())
                Thread.Sleep(100);
        }
        try
        {
            while (logs.Count > 250)
            {
                logs.RemoveAt(0);
            }
            logs.Add(Log.FormatLog(args));
        }
        finally
        {
            mutex.Unlock();
        }
        CallDeferred(nameof(OnLogged));
    }

    private void OnLogged()
    {
        _logText = string.Join('\n', logs);
        if (IsInstanceValid(label))
            label.Text = _logText;
        EmitSignal(SignalName.OnLogSetText, _logText);
    }
}
