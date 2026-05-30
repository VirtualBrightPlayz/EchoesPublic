using System;
using System.Text;
using System.Threading.Tasks;
using Godot;
using GodotSteam;

[GlobalClass]
public partial class LobbyListingUI : PanelContainer, IServerListing
{
    public ulong ListInfo;

    [Export] public TextureRect Icon;
    [Export] public RichTextLabel Title;
    [Export] public Button JoinBtn;

    public ServerListUI ServerList { get; set; }
    public string ServerName { get; set; }
    public string[] ServerTags => [ServerVersion];
    public int ServerCurrentPlayerCount { get; set; }
    public int ServerMaxPlayerCount { get; set; }
    public string ServerVersion;

    public override void _Ready()
    {
        base._Ready();
        JoinBtn.Pressed += ServerJoin;
        Title.Text = $"{ServerName} ({ServerCurrentPlayerCount}/{ServerMaxPlayerCount})";
        LoadIcon();
    }

    public void Setup(ulong id)
    {
        ListInfo = id;
        Steam.RequestLobbyData(ListInfo);
        ServerName = Steam.GetLobbyData(ListInfo, "name");
        ServerCurrentPlayerCount = Steam.GetNumLobbyMembers(ListInfo);
        ServerMaxPlayerCount = Steam.GetLobbyMemberLimit(ListInfo);
        ServerVersion = Steam.GetLobbyData(ListInfo, "version");
    }

    public void LoadIcon()
    {
        string owner = Steam.GetLobbyData(ListInfo, "owner");
        if (ulong.TryParse(owner, out var ownerId))
        {
            SteamManager.Instance.GetAvatar(ownerId, img =>
            {
                if (IsInstanceValid(this) && IsInstanceValid(Icon) && IsInstanceValid(img))
                {
                    Icon.Texture = ImageTexture.CreateFromImage(img);
                }
            });
        }
    }

    public void ServerJoin()
    {
        Steam.JoinLobby(ListInfo);
    }
}
