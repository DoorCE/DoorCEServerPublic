using System.Globalization;
using DoorCEModel.Infrastructure.DataModel.Datasets;

namespace DoorCEServer.Application.DataContentsManager.Common;

public static class MDataContentsManagerCommon
{
    // keep track of used integer IDs to assign new ones if needed
    public static HashSet<int> GetUsedIntegerIds(Dataset dataset)
    {
        return dataset.Items.Where(i => Int32.TryParse(i.Identifier, out _))
            .Select(i => Int32.Parse(i.Identifier)).ToHashSet();
    }

    // find next available integer id not present in the set
    public static int GetNextIntegerId(HashSet<int> usedIntegerIds, int startingId = 1)
    {
        int firstMissingNumber = startingId;
        while (usedIntegerIds.Contains(firstMissingNumber)) {
            firstMissingNumber++;
        }
        return firstMissingNumber;
    }
    
    public static object? ParsePrimitive(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return token;
        var t = token.Trim();

        if (string.Equals(t, "null", StringComparison.OrdinalIgnoreCase)) return null;
        
        // if (TryParseTuple(t, out var tuple)) return tuple;

        if (bool.TryParse(t, out var b)) return b;
        
        if (DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)) return dt;

        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
        if (long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return l;
        if (double.TryParse(t, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d)) return d;

        // remove surrounding quotes if present
        if ((t.StartsWith("\"") && t.EndsWith("\"")) || (t.StartsWith("'") && t.EndsWith("'")))
        {
            if (t.Length >= 2) return t.Substring(1, t.Length - 2);
        }

        return t;
    }
    
    public static bool TryParseTuple(string input, out (double, double) result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(input))
            return false;
        
        if (!input.Trim().StartsWith("(") || !input.Trim().EndsWith(")"))
        {
            return false;
        }

        input = input.Trim()[1..^1]; // remove the surrounding parentheses
        var parts = input.Split(',').Select(p => p.Trim()).ToArray();
        
        if (parts.Length != 2)
            return false;

        if ((double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var first) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var second))
            || (double.TryParse(parts[0], NumberStyles.Float, new CultureInfo("pl-PL"), out first) &&
                double.TryParse(parts[1], NumberStyles.Float, new CultureInfo("pl-PL"), out second)))
        {
            result = (first, second);
            return true;
        }

        return false;
    }

    public static IEnumerable<string> SplitArrayElements(string input)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();

        char? quote = null;
        int parenthesesDepth = 0;

        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            
            if (c == '"' || c == '\'')
            {
                if (quote == null)
                {
                    quote = c;
                    continue;
                }
                else if (quote == c)
                {
                    quote = null;
                    continue;
                }
            }

            if (quote == null)
            {
                if (c == '(')
                {
                    parenthesesDepth++;
                }
                else if (c == ')')
                {
                    parenthesesDepth--;
                }
            }
            
            if (c == ',' && quote == null && parenthesesDepth == 0)
            {
                parts.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        parts.Add(current.ToString().Trim());
        return parts;
    }
}