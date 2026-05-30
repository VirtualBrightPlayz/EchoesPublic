using System;
using System.Linq;
using Godot;

[GlobalClass]
public partial class KeycardUpgrade : ItemUpgradeRecipe
{
    /*
    public override bool IsValidFor(ItemPreset item, out ItemPreset put)
    {
        put = null;
        if (item is Keycard keycard)
        {
            int level = keycard.access.level;
            switch (setting)
            {
                case SCP914Knob.KnobSetting.Coarse:
                    level--;
                    break;
                case SCP914Knob.KnobSetting.Fine:
                    level++;
                    break;
                default:
                    return false;
            }
            InventoryItem output = ItemManager.Instance.Data.Items.Where(x => x is Keycard).Select(x => x as Keycard).FirstOrDefault(x => x.access.cardType == keycard.access.cardType && x.access.level == level);
            put = output;
            return IsInstanceValid(output);
        }
        return false;
    }
    */
}
