using Godot;
using Tomlyn;
using Tomlyn.Model;
using static TomlExtensions;

[GlobalClass]
public partial class PlayerAbility : Resource
{
    [Export]
    public string file;
    [Export]
    public string BaseAbility;
    public PackedScene Scene => IInitScript.Instance.Data.PlayerAbilities.TryGetValue(BaseAbility, out PackedScene scn) ? scn : null;
    public TomlTable cfg;
    public TomlTable abilityCfg;
    public TomlTable dataCfg;

    public bool ReadFromFile()
    {
        PlayerAbility role = this;
        if (string.IsNullOrEmpty(file))
        {
            return false;
        }
        FileAccess fs = FileAccess.Open(file, FileAccess.ModeFlags.Read);
        if (!IsInstanceValid(fs))
        {
            return false;
        }
        cfg = TomlSerializer.Deserialize<TomlTable>(fs.GetAsText());
        fs.Close();
        // role
        if (TryGetTable(cfg, "ability", out abilityCfg))
        {
            role.ResourceName = (string)abilityCfg["name"];
            role.BaseAbility = (string)abilityCfg["base_name"];
            return TryGetTable(abilityCfg, "data", out dataCfg);
        }
        return false;
    }
}
