using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;

[GlobalClass]
public partial class ChangelogLabel : Node
{
    [Signal]
    public delegate void OnLoadedEventHandler();

    public RichTextLabel ParentText => GetParent<RichTextLabel>();

    [Export]
    public int titleSize = 24;
    [Export]
    public int bodySize = 12;
    [Export]
    public Color fontColor = Colors.White;

    public static string Title = string.Empty;
    public static string Body = string.Empty;
    public static string Url = string.Empty;

    public override async void _Ready()
    {
        try
        {
            byte[] data = await HttpUtils.HttpGetAsync(this, "https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=3630170");
            string str = data.GetStringFromUtf8();
            var news = JsonNode.Parse(str)["appnews"]["newsitems"].AsArray();
            Url = (string)news[0]["url"];
            Title = (string)news[0]["title"];
            string b = (string)news[0]["contents"];
            Body = b.Replace("[p]", string.Empty).Split("[/p]").FirstOrDefault() ?? string.Empty;

            ParentText.Clear();
            ParentText.PushColor(fontColor);

            ParentText.PushFontSize(titleSize);
            ParentText.PushParagraph(HorizontalAlignment.Center);
            ParentText.AddText(Title);
            ParentText.Pop();
            ParentText.Pop();

            ParentText.PushFontSize(bodySize);
            ParentText.PushParagraph(HorizontalAlignment.Left);
            ParentText.AddText(Body);
            ParentText.Pop();
            ParentText.Pop();

            ParentText.Pop();

            EmitSignalOnLoaded();
        }
        catch (Exception e)
        {
            Log.PrintErr(e);
        }
    }

    public void OpenUrl()
    {
        if (Url.StartsWith("https://"))
            OS.ShellOpen(Url);
    }
}