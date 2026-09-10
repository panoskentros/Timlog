using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Timlog.Domain.Enums;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        var member = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();
        if (member != null)
        {
            var attribute = member.GetCustomAttribute<DisplayAttribute>();
            if (attribute != null && attribute.Name != null)
            {
                return attribute.Name;
            }
        }

        return enumValue.ToString();
    }
}