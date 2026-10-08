namespace DoorCEModel.Utils;

public class IdentifiableElementUtils
{
    // TODO - refactor to use this method throughout (wherever applicable)
    public static string SelectByLanguage(Dictionary<string, string> strings, string language)
    {
        return strings.ContainsKey(language) ? strings[language] :
            strings.ContainsKey("en") ? strings["en"] :
            strings.Values.First();
    }
}