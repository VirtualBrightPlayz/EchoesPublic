using System;
using System.Text;
using System.Threading.Tasks;
using Godot;

public interface IServerListing
{
    ServerListUI ServerList { get; set; }
    string ServerName { get; }
    string[] ServerTags { get; }
    int ServerCurrentPlayerCount { get; }
    int ServerMaxPlayerCount { get; }
    void ServerJoin();
}

[GlobalClass]
public partial class ServerListingUI : PanelContainer, IServerListing
{
    public NetworkManager.ServerListPost ListInfo;

    [Export] public TextureRect Icon;
    [Export] public RichTextLabel Title;
    [Export] public Button InfoBtn;
    [Export] public Button JoinBtn;

    public ServerListUI ServerList { get; set; }
    public string ServerName => ListInfo.name;
    public string[] ServerTags => ListInfo.tags.Split(',');
    public int ServerCurrentPlayerCount => ListInfo.players;
    public int ServerMaxPlayerCount => ListInfo.maxPlayers;

    public override void _Ready()
    {
        base._Ready();
        InfoBtn.Pressed += _Info;
        JoinBtn.Pressed += ServerJoin;
        Title.Text = $"{ServerName} ({ServerCurrentPlayerCount}/{ServerMaxPlayerCount})";
        InfoBtn.Visible = !string.IsNullOrEmpty(ListInfo.infoUrl);
        _ = LoadIcon();
    }

    public async Task LoadIcon()
    {
        if (string.IsNullOrWhiteSpace(ListInfo.iconUrl))
            return;
        try
        {
            byte[] data = await HttpUtils.HttpGetAsync(this, ListInfo.iconUrl);
            if (IsInstanceValid(Icon))
            {
                Image img = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
                if (img.LoadPngFromBuffer(data) == Error.Ok)
                {
                    Icon.Texture = ImageTexture.CreateFromImage(img);
                }
                else if (img.LoadJpgFromBuffer(data) == Error.Ok)
                {
                    Icon.Texture = ImageTexture.CreateFromImage(img);
                }
            }
        }
        catch (Exception e)
        {
            Log.PrintErr(e);
        }
    }

    private async void _Info()
    {
        ServerList.StartViewInfo(this);
        if (string.IsNullOrWhiteSpace(ListInfo.infoUrl))
        {
            ServerList.EndViewInfo(this, string.Empty);
            return;
        }
        try
        {
            string info = Encoding.UTF8.GetString(await HttpUtils.HttpGetAsync(this, ListInfo.infoUrl));
            ServerList.EndViewInfo(this, info);
        }
        catch (Exception e)
        {
            Log.PrintErr(e);
            ServerList.EndViewInfo(this, "Error getting server info. Check console for details.");
        }
    }

    public void ServerJoin()
    {
        MenuManager.Instance.Join(ListInfo.address, ListInfo.name);
    }
}
