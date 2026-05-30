using System;
using Godot;

public partial class CreditsViewer : RichTextLabel
{
    public const string CreditsCsvPath = "res://credits.csv";
    public const string RichCreditsPath = "res://credits.txt";
    public const string ConsoleCreditsPath = "res://console_credits.txt";

    [Export(PropertyHint.MultilineText)]
    public string BaseFormat;

    [Export(PropertyHint.MultilineText)]
    public string CellFormat;

    public override void _EnterTree()
    {
        VisibilityChanged += Vis;
        MetaClicked += Meta;
        Vis();
    }

    public override void _ExitTree()
    {
        VisibilityChanged -= Vis;
        MetaClicked -= Meta;
    }

    private void Meta(Variant meta)
    {
        string url = meta.AsString();
        OS.ShellOpen(url);
    }

    private void Vis()
    {
        Text = "Credits Error";
        Text = GetRichCredits();
    }

    public static string GetRichCredits()
    {
        using var file = FileAccess.Open(RichCreditsPath, FileAccess.ModeFlags.Read);
        string txt = file.GetAsText(true);
        string[] split = txt.Split("///");
        return GetCredits(split[0].Trim(), split[1].Trim());
    }

    public static string GetConsoleCredits()
    {
        using var file = FileAccess.Open(ConsoleCreditsPath, FileAccess.ModeFlags.Read);
        string txt = file.GetAsText(true);
        string[] split = txt.Split("///");
        return GetCredits(split[0].Trim(), split[1].Trim());
    }

    public static string GetCredits(string BaseFormat, string CellFormat)
    {
        using var file = FileAccess.Open(CreditsCsvPath, FileAccess.ModeFlags.Read);
        string txt = "";
        while (!file.EofReached())
        {
            string[] line = file.GetCsvLine();
            try
            {
                txt += string.Format(CellFormat, line).Replace("\\n", "\n") + '\n';
            }
            catch (FormatException)
            {
                Log.Print("CSV Credits line not formatted, \"", string.Join(' ', line), "\".");
            }
        }
        return string.Format(BaseFormat, txt);
    }
}
