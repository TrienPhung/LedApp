using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

public static class EnumHelper
{
    public static string GetDisplayName(Enum enumValue)
    {
        var member = enumValue.GetType().GetMember(enumValue.ToString());
        var attr = member[0].GetCustomAttribute<DisplayAttribute>();

        return attr?.Name ?? enumValue.ToString();
    }
}