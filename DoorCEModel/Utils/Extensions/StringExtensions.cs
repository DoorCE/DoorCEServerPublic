using System.Globalization;

namespace DoorCEModel.Utils.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Always returns the same 64-bits hash code for an identical string
    /// </summary>
    /// <returns>Hash code</returns>
    public static long GetDeterministicHashCode(this string str)
    {
        unchecked
        {
            var hash1 = 5381L;
            var hash2 = hash1;

            for (var i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ str[i];
                if (i == str.Length - 1)
                {
                    break;
                }
                hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }

    /// <summary>
    /// Always returns the same 32-bits hash code for an identical string
    /// </summary>
    /// <returns>Hash code</returns>
    public static int GetDeterministicHashCode32(this string str)
    {
        unchecked
        {
            var hash1 = 5381;
            var hash2 = hash1;

            for (var i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1)
                {
                    break;
                }
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }

    public static string ToPascalCase(this string str)
    {
        TextInfo Ti = new CultureInfo("en-UK",false).TextInfo;
        return Ti.ToTitleCase(str).Replace(" ", "");
    }
    
    public static string ToCamelCase(this string str)
    {
        string output = str.ToPascalCase();
        return output[0..1].ToLower() + output[1..];
    }
    
    public static string ToSnakeCase(this string str)
    {
        TextInfo Ti = new CultureInfo("en-UK",false).TextInfo;
        return Ti.ToLower(str).Replace(" ", "_");
    }
    
    public static bool NamingEquals(this string str, string? other)
    {
        return string.Equals(str, other, StringComparison.OrdinalIgnoreCase);
    }
}