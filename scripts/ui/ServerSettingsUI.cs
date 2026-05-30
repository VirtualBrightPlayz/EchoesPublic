using System.Linq;
using Godot;

public partial class ServerSettingsUI : Node
{
    public enum SettingType : int
    {
        ServerName,
        ServerPort,
        NetworkingType,
        MinPlayers,
        MaxPlayers,
        PublicLobby,
        Map,
    }

    [Export]
    public SettingType settingType;

    public override void _Ready()
    {
        switch (settingType)
        {
            case SettingType.ServerName:
                GetParent<LineEdit>().TextChanged += _ => WriteTo();
                break;
            case SettingType.ServerPort:
            case SettingType.MinPlayers:
            case SettingType.MaxPlayers:
                GetParent<Range>().ValueChanged += _ => WriteTo();
                break;
            case SettingType.NetworkingType:
            case SettingType.Map:
                GetParent<OptionButton>().ItemSelected += _ => WriteTo();
                break;
            case SettingType.PublicLobby:
                GetParent<BaseButton>().Toggled += _ => WriteTo();
                break;
        }
        ReadFrom();
    }
    
    public void ReadFrom()
    {
        switch (settingType)
        {
            case SettingType.ServerName:
            {
                GetParent<LineEdit>().Text = Settings.Server.ServerName;
                break;
            }
            case SettingType.ServerPort:
            {
                GetParent<Range>().SetValueNoSignal(Settings.Server.ServerPort);
                break;
            }
            case SettingType.MinPlayers:
            {
                GetParent<Range>().SetValueNoSignal(Settings.Server.MinPlayers);
                break;
            }
            case SettingType.MaxPlayers:
            {
                GetParent<Range>().SetValueNoSignal(Settings.Server.MaxPlayers);
                break;
            }
            case SettingType.PublicLobby:
            {
                GetParent<BaseButton>().SetPressedNoSignal(MenuManager.Instance.steamPublicLobby);
                break;
            }
            case SettingType.Map:
            {
                string path = "user://maps";
                if (!DirAccess.DirExistsAbsolute(path))
                    DirAccess.MakeDirAbsolute(path);
                DirAccess dir = DirAccess.Open(path);
                string[] files = dir.GetFiles().Where(x => x.GetExtension().Equals("json", System.StringComparison.InvariantCultureIgnoreCase)).Select(x => path.PathJoin(x)).ToArray();
                string[] dirs = dir.GetDirectories().Select(x => path.PathJoin(x)).ToArray();
                var parent = GetParent<OptionButton>();
                parent.Clear();
                parent.AddItem("Random");
                foreach (var file in files)
                {
                    parent.AddItem(file);
                    string[] fs = [file];
                    parent.SetItemMetadata(parent.ItemCount - 1, fs);
                }
                foreach (var mod in ModLoader.Instance.Mods.Where(x => x.Loaded))
                {
                    DirAccess otherDir = DirAccess.Open(mod.MapsPath);
                    if (!IsInstanceValid(otherDir))
                    {
                        continue;
                    }
                    string[] fs = otherDir.GetFiles().Where(x => x.GetExtension().Equals("json", System.StringComparison.InvariantCultureIgnoreCase)).Select(x => mod.MapsPath.PathJoin(x)).ToArray();
                    if (fs.Length == 0)
                    {
                        continue;
                    }
                    parent.AddItem(mod.Info.Name);
                    parent.SetItemMetadata(parent.ItemCount - 1, fs);
                }
                parent.Selected = 0;
                for (int i = 0; i < parent.ItemCount; i++)
                {
                    Variant data = parent.GetItemMetadata(i);
                    if (data.VariantType != Variant.Type.PackedStringArray)
                    {
                        continue;
                    }
                    string[] arr = data.AsStringArray();
                    if (arr.Length > 0 && arr.SequenceEqual(Settings.Server.CustomMaps))
                    {
                        parent.Selected = i;
                        break;
                    }
                }
                break;
            }
        }
    }

    public void WriteTo()
    {
        switch (settingType)
        {
            case SettingType.ServerName:
            {
                Settings.Server.ServerName = GetParent<LineEdit>().Text;
                break;
            }
            case SettingType.ServerPort:
            {
                Settings.Server.ServerPort = (int)GetParent<Range>().Value;
                break;
            }
            case SettingType.MinPlayers:
            {
                Settings.Server.MinPlayers = (int)GetParent<Range>().Value;
                break;
            }
            case SettingType.MaxPlayers:
            {
                Settings.Server.MaxPlayers = (int)GetParent<Range>().Value;
                break;
            }
            case SettingType.PublicLobby:
            {
                MenuManager.Instance.steamPublicLobby = GetParent<BaseButton>().ButtonPressed;
                break;
            }
            case SettingType.Map:
            {
                int sel = GetParent<OptionButton>().Selected;
                if (sel > 0)
                {
                    string[] path = GetParent<OptionButton>().GetItemMetadata(sel).AsStringArray();
                    Settings.Server.CustomMaps = path;
                }
                else
                {
                    Settings.Server.CustomMaps = System.Array.Empty<string>();
                }
                break;
            }
        }
        Settings.WriteServerSettings();
    }
}