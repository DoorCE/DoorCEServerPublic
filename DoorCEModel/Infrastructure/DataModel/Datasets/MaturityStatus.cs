using System.ComponentModel;
using System.Reflection;

namespace DoorCEModel.Infrastructure.DataModel.Datasets;

public enum MaturityStatus // TODO - review
{
    [Description("COMPLETED")]
    Completed,
    [Description("DEPRECATED")]
    Deprecated,
    [Description("DEVELOPED")]
    Developed,
    [Description("WITHDRAWN")]
    Withdrawn,
    [Description("DISCONTINUED")]
    Discontinued
}

public static class MaturityStatusHelper
{
    public static string ToDescriptionString(MaturityStatus value)
    {
        var attribute =
            value.GetType()
                    .GetTypeInfo()
                    .GetMember(value.ToString())
                    .FirstOrDefault(member => member.MemberType == MemberTypes.Field)?
                    .GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .SingleOrDefault()
                as DescriptionAttribute;

        return attribute?.Description ?? value.ToString();
    }

    public static MaturityStatus FromDescriptionString(string? value)
    {
        if (null == value) return MaturityStatus.Completed;
        return value switch
        {
            "COMPLETED" => MaturityStatus.Completed,
            "DEPRECATED" => MaturityStatus.Deprecated,
            "DEVELOPED" => MaturityStatus.Developed,
            "WITHDRAWN" => MaturityStatus.Withdrawn,
            "DISCONTINUED" => MaturityStatus.Discontinued,
            _ => throw new ArgumentException($"Unknown value: {value}", nameof(value))
        };
    }
}