using System.Diagnostics.CodeAnalysis;

namespace DoorCEServer.Utils.Extensions;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{
    public CallerArgumentExpressionAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }

    public string ParameterName { get; }
}

internal static class Guard
{
    public static T ThrowIfNull<T>([NotNull] T? argument, bool throwOnEmptyString = false, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
        if (throwOnEmptyString && argument is string s && string.IsNullOrEmpty(s))
            throw new ArgumentNullException(paramName);
        return argument;
    }
}