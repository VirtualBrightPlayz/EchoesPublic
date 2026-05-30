using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

public static partial class Log
{
    public enum LogLevel : byte
    {
        Debug,
        Info,
        Warning,
        Error,
    }

    public class LogEventArgs : EventArgs
    {
        public readonly LogLevel Level;
        public readonly string Prefix;
        public readonly string Text;
        public readonly object[] Raw;
        public readonly StackFrame Stack;
        public readonly DateTime TimestampUTC;
        public readonly DateTime TimestampLocal;

        public LogEventArgs()
        {
        }

        public LogEventArgs(string separator, object[] args, StackFrame trace = null)
        {
            TimestampUTC = DateTime.UtcNow;
            TimestampLocal = DateTime.Now;
            Stack = trace ?? new StackFrame(1);
            // Stack = trace ?? new StackTrace(1, true);
            Level = LogLevel.Info;
            Prefix = FindPrefix();
            Raw = args.Clone() as object[];
            Text = string.Join(separator, Raw);
        }

        public LogEventArgs(LogLevel level, string separator, object[] args, StackFrame trace = null)
        {
            TimestampUTC = DateTime.UtcNow;
            TimestampLocal = DateTime.Now;
            Stack = trace ?? new StackFrame(1);
            // Stack = trace ?? new StackTrace(1, true);
            Level = level;
            Prefix = FindPrefix();
            Raw = args.Clone() as object[];
            Text = string.Join(separator, Raw);
        }

        public string FindPrefix()
        {
            string asmName = Assembly.GetCallingAssembly().GetName().Name;
            StackFrame frame = Stack;
            if (Settings.User.DebugMode && frame != null && frame.HasMethod())
            {
                MethodBase method = frame.GetMethod();
                Type t = method.DeclaringType;
                if (t != null)
                {
                    while (t.DeclaringType != null)
                    {
                        t = t.DeclaringType;
                    }
                    return asmName + "::" + t.Name;
                }
            }
            return asmName;
        }

        public string ConsoleColorFormatted()
        {
            const string search = "[color=";
            string str = string.Empty;
            string token = string.Empty;
            for (int i = 0; i < Text.Length; i++)
            {
                if (Text[i] == '[' || !string.IsNullOrEmpty(token))
                    token += Text[i];
                else
                    str += Text[i];

                if (Text[i] == ']')
                {
                    if (token.StartsWith(search))
                    {
                        string colorStr = token.Substring(search.Length, token.Length - search.Length - 1);
                        Color color = new Color(colorStr);
                        str += search + color.FormatLogColor() + ']';
                    }
                    else
                    {
                        str += token;
                    }
                    token = string.Empty;
                }
            }
            if (!string.IsNullOrEmpty(token))
                str += token;
            return str;
        }
    }

    public static readonly string[] ValidConsoleColorNames = new string[]
    {
        "black",
        "red",
        "green",
        "yellow",
        "blue",
        "magenta",
        "pink",
        "purple",
        "cyan",
        "white",
        "orange",
        "gray",
    };

    public static readonly List<string> BlockedPrefixes = new List<string>();
    public static readonly List<string> NeverDupelicatePrefixes = new List<string>();
    public static bool ShowLogType = true;
    public static bool RichTextLogs = true;
    public static bool ShowTimestamps = true;
    public static bool TimestampsInUTC = true;
    public static int MaxDuplicateLogs = 10;
    public static TimeSpan DuplicateLogsTimeWindow = new TimeSpan(0, 1, 0);
    public static LogLevel MinLogLevel = LogLevel.Info;

    static Log()
    {
        if (EngineDebugger.IsActive())
        {
            MinLogLevel = LogLevel.Debug;
            MaxDuplicateLogs = 100;
            // DuplicateLogsTimeWindow = new TimeSpan(0, 0, 10);
        }
    }

    private static readonly Dictionary<string, List<LogEventArgs>> _RecentLogs = new Dictionary<string, List<LogEventArgs>>();

    public static event EventHandler<LogEventArgs> OnLogQueued;
    public static event EventHandler<LogEventArgs> OnLogPrinted;

    public static List<string> BypassMaxLogsRestriction = new List<string>();
    
    private static void OnLogged(object sender, LogEventArgs args)
    {
        OnLogQueued?.Invoke(sender, args);
        if (args.Level < MinLogLevel)
            return;
        if (!_RecentLogs.ContainsKey(args.Prefix))
            _RecentLogs.Add(args.Prefix, new List<LogEventArgs>());
        int count = _RecentLogs[args.Prefix].RemoveAll(x => x.TimestampUTC + DuplicateLogsTimeWindow <= DateTime.UtcNow);
        _RecentLogs[args.Prefix].Add(args);
        if (!NeverDupelicatePrefixes.Contains(args.Prefix))
        {
            /*
            if (_RecentLogs[args.Prefix].Count == MaxDuplicateLogs && !BypassMaxLogsRestriction.Contains(args.Prefix))
            {
                FormatAndLog(sender, new LogEventArgs(LogLevel.Warning, "", new[] { $"Too many logs in {DuplicateLogsTimeWindow}, stopping output for \"{args.Prefix}\"." }, args.Stack));
                return;
            }
            if (_RecentLogs[args.Prefix].Count > MaxDuplicateLogs)
                return;
            */
        }
        if (BlockedPrefixes.Contains(args.Prefix))
            return;
        FormatAndLog(sender, args);
    }

    private static void FormatAndLog(object sender, LogEventArgs args)
    {
        string finalText = FormatLog(args);
        if (RichTextLogs)
            GD.PrintRich(finalText);
        else
            GD.Print(finalText);
        OnLogPrinted?.Invoke(sender, args);
    }

    public static string FormatLog(LogEventArgs args)
    {
        string format = "[color={3}][b]{0}{1}:[/b] {2}[/color]";
        if (!RichTextLogs)
            format = "{0}{1}: {2}";

        string logTypeFormat = string.Empty;
        string logColorFormat = "white";
        if (ShowLogType)
        {
            switch (args.Level)
            {
                case LogLevel.Debug:
                    logTypeFormat = "DEBUG: ";
                    break;
                case LogLevel.Info:
                    logTypeFormat = "INFO: ";
                    logColorFormat = "green";
                    break;
                case LogLevel.Warning:
                    logTypeFormat = "WARN: ";
                    logColorFormat = "yellow";
                    break;
                case LogLevel.Error:
                    logTypeFormat = "ERROR: ";
                    logColorFormat = "red";
                    break;
            }
        }
        if (ShowTimestamps)
        {
            if (TimestampsInUTC)
                logTypeFormat = $"[{args.TimestampUTC.ToLongTimeString()} UTC] " + logTypeFormat;
            else
                logTypeFormat = $"[{args.TimestampLocal.ToLongTimeString()}] " + logTypeFormat;
        }

        string finalText = string.Format(format, logTypeFormat, args.Prefix, args.ConsoleColorFormatted(), logColorFormat);

        return finalText;
    }

    public static void ClearRecentLogs()
    {
        _RecentLogs.Clear();
    }

    public static string FormatLogColor(this Color color)
    {
        if (IInitScript.IsServerOnly)
        {
            var bDist = new Vector3(color.R, color.G, color.B);

            var closest = Colors.White;
            var name = nameof(Colors.White);
            var dist = new Vector3(1f, 1f, 1f);
            var props = typeof(Colors).GetProperties();
            foreach (var prop in props)
            {
                if (Array.IndexOf(ValidConsoleColorNames, prop.Name.ToLower()) != -1 && prop.PropertyType == typeof(Color))
                {
                    var col = (Color)prop.GetValue(null);
                    var d = new Vector3(col.R, col.G, col.B);
                    if (bDist.DistanceSquaredTo(d) < bDist.DistanceSquaredTo(dist))
                    {
                        closest = col;
                        name = prop.Name;
                        dist = d;
                    }
                }
            }
            return name.ToLower();
        }
        return $"#{color.ToHtml()}";
    }

    public static void PrintS(params object[] args)
    {
        OnLogged(null, new LogEventArgs(LogLevel.Debug, " ", args, new StackFrame(1)));
    }

    public static void PrintInfoS(params object[] args)
    {
        OnLogged(null, new LogEventArgs(LogLevel.Info, " ", args, new StackFrame(1)));
    }

    public static void PrintWarnS(params object[] args)
    {
        OnLogged(null, new LogEventArgs(LogLevel.Warning, " ", args, new StackFrame(1)));
    }

    public static void PrintErrS(params object[] args)
    {
        OnLogged(null, new LogEventArgs(LogLevel.Error, " ", args, new StackFrame(1)));
    }

    public static void Print(params object[] args)
    {
        OnLogged(null, new LogEventArgs(LogLevel.Debug, "", args, new StackFrame(1)));
    }

    public static void PrintInfo(params object[] args)
    {
        OnLogged(null, new LogEventArgs(LogLevel.Info, "", args, new StackFrame(1)));
    }

    public static void PrintWarn(params object[] args)
    {
        OnLogged(null, new LogEventArgs(LogLevel.Warning, "", args, new StackFrame(1)));
    }

    public static void PrintErr(params object[] args)
    {
        OnLogged(null, new LogEventArgs(LogLevel.Error, "", args, new StackFrame(1)));
    }
}