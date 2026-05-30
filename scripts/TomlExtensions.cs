using System;
using Tomlyn.Model;

public static class TomlExtensions
{
    public static void GetValueOptional<T>(TomlTable table, string key, ref T value) where T : IConvertible
    {
        if (TryGetValue(table, key, out T temp))
        {
            value = temp;
        }
    }

    public static void GetArrayOptional<T>(TomlTable table, string key, ref T[] value) where T : IConvertible
    {
        if (TryGetArray(table, key, out T[] temp))
        {
            value = temp;
        }
    }

    public static bool TryGetValue<T>(TomlTable table, string key, out T value) where T : IConvertible
    {
        if (table != null && table.TryGetValue(key, out object obj))
        {
            if (obj is T t)
            {
                value = t;
                return true;
            }
            if (obj is string str && typeof(T).IsEnum)
            {
                if (Enum.TryParse(typeof(T), str, out var result))
                {
                    value = (T)result;
                    return true;
                }
                value = default;
                return false;
            }
            value = (T)Convert.ChangeType(obj, typeof(T));
            return true;
        }
        value = default;
        return false;
    }

    public static bool TryGetArray<T>(TomlTable table, string key, out T[] value) where T : IConvertible
    {
        if (table != null && table.TryGetValue(key, out object obj))
        {
            if (obj is T[] t)
            {
                value = t;
                return true;
            }
            TomlArray arr = (TomlArray)obj;
            value = new T[arr.Count];
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] is string str && typeof(T).IsEnum)
                {
                    if (Enum.TryParse(typeof(T), str, out var result))
                    {
                        value[i] = (T)result;
                    }
                }
                else
                {
                    value[i] = (T)Convert.ChangeType(arr[i], typeof(T));
                }
            }
            return true;
        }
        value = default;
        return false;
    }

    public static bool TryGetTable(TomlTable table, string key, out TomlTable value)
    {
        if (table != null && table.TryGetValue(key, out object obj))
        {
            if (obj is TomlTable t)
            {
                value = t;
                return true;
            }
        }
        value = default;
        return false;
    }

    public static bool TryGetTableArray(TomlTable table, string key, out TomlTableArray value)
    {
        if (table != null && table.TryGetValue(key, out object obj))
        {
            if (obj is TomlTableArray t)
            {
                value = t;
                return true;
            }
        }
        value = default;
        return false;
    }
}