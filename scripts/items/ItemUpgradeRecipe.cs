using System.Linq;
using Godot;
using Tomlyn;
using Tomlyn.Model;

[GlobalClass]
public partial class ItemUpgradeRecipe : Resource
{
    [Export]
    public string file;
    [Export]
    public SCP914Knob.KnobSetting setting;
    [Export]
    public ItemPreset input;
    public ItemPreset inputItem => IInitScript.Instance.Data.ItemPresets.FirstOrDefault(x => x.ResourceName == inputItemName) ?? input;
    [Export]
    public string inputItemName;
    [Export]
    public ItemPreset output;
    public ItemPreset outputItem => IInitScript.Instance.Data.ItemPresets.FirstOrDefault(x => x.ResourceName == outputItemName) ?? output;
    [Export]
    public string outputItemName;
    [Export(PropertyHint.Range, "0,1")]
    public float failChance = 0f;
    [Export(PropertyHint.Range, "0,1")]
    public float destroyOnFailChance = 0f;

    public bool ReadFromFile()
    {
        FileAccess fs = FileAccess.Open(file, FileAccess.ModeFlags.Read);
        if (!IsInstanceValid(fs))
        {
            return false;
        }
        TomlTable cfg = TomlSerializer.Deserialize<TomlTable>(fs.GetAsText());
        fs.Close();
        if (TomlExtensions.TryGetTable(cfg, "item_upgrade", out TomlTable upgradeCfg))
        {
            return ReadFromTomlTable(upgradeCfg);
        }
        return false;
    }

    public bool ReadFromTomlTable(TomlTable upgradeCfg)
    {
        TomlExtensions.GetValueOptional(upgradeCfg, "scp_914_setting", ref setting);
        TomlExtensions.GetValueOptional(upgradeCfg, "input_item", ref inputItemName);
        TomlExtensions.GetValueOptional(upgradeCfg, "output_item", ref outputItemName);
        TomlExtensions.GetValueOptional(upgradeCfg, "fail_chance", ref failChance);
        TomlExtensions.GetValueOptional(upgradeCfg, "destroy_on_fail_chance", ref destroyOnFailChance);
        return true;
        // return !string.IsNullOrEmpty(inputItemName) && !string.IsNullOrEmpty(outputItemName);
    }

    public virtual bool IsValidFor(ItemPreset item, out ItemPreset put)
    {
        put = outputItem;
        if (GD.Randf() <= failChance)
        {
            put = inputItem;
            if (GD.Randf() <= destroyOnFailChance)
            {
                put = null;
            }
        }
        if (!IsInstanceValid(inputItem))
            return true;
        if (item == inputItem || inputItem.Id == item.Id)
        {
            return true;
        }
        return false;
    }
}
