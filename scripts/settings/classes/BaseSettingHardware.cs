using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;

public abstract partial class BaseSettingHardware<T> : Dropdown, IHardwareDetector<T>
{
    public abstract HardwareDetectorType DetectorType { get; }

    public abstract IEnumerable<T> GetAvailableHardware();

    public abstract IEnumerable<string> GetAvailableHardwareNames();
    
    public abstract T Default { get; }

    public abstract string DefaultName { get; }

    public abstract int ConvertToIndex(T value);

    public virtual bool AddDefaultToDropdown { get; } = true;

    public override void Save()
    {
        int val = (int)Convert.ChangeType(Value, typeof(int));
        T valToUse = Default;
        bool changed = false;
        if (val != -1 && (!AddDefaultToDropdown || val != 0))
        {
            valToUse = GetAvailableHardware().ElementAt((val));
        }
        if (Setting.Info.MemberType == MemberTypes.Field)
        {
            var info = Setting.Info as FieldInfo;
            if (!((T)info.GetValue(Setting.SettingsObject)).Equals(valToUse))
            {
                changed = true;
            }
            info.SetValue(Setting.SettingsObject, valToUse);
        }
        else if (Setting.Info.MemberType == MemberTypes.Property)
        {
            var info = Setting.Info as PropertyInfo;
            if (!((T)info.GetValue(Setting.SettingsObject)).Equals(valToUse))
            {
                changed = true;
            }
            info.SetValue(Setting.SettingsObject, valToUse);
        }
        if (changed && Setting?.Info?.GetCustomAttribute<RestartRequiredAttribute>() != null)
        {
            ShowMessage(MessageLevel.Information, "Restart is required to apply this setting.");
        }
        InvokeSaved(Value);
    }

    public override void Load()
    {
        //base.Load();
        string[] hardware = GetAvailableHardwareNames().ToArray();
        DropDown.Clear();
        if (AddDefaultToDropdown)
        {
            DropDown.AddItem(DefaultName);
        }
        for (int i = 0; i < hardware.Length; i++)
        {
            DropDown.AddItem(hardware[i]);
        }
        T val = Default;
        if (Setting.Info.MemberType == MemberTypes.Field)
        {
            var info = Setting.Info as FieldInfo;
            val = (T)info.GetValue(Setting.SettingsObject);
        }
        else if (Setting.Info.MemberType == MemberTypes.Property)
        {
            var info = Setting.Info as PropertyInfo;
            val = (T)info.GetValue(Setting.SettingsObject);
        }
        int selected = ConvertToIndex(val);
        DropDown.Selected = selected;
        InvokeLoaded(Value);
    }

    public override void Reset()
    {
        //base.Reset();
        Value = ConvertToIndex(Default);
        Save();
    }
}
