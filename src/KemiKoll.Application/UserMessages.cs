namespace KemiKoll.Application;

/// <summary>
/// Turns domain exceptions into messages that can be shown to a user.
/// </summary>
internal static class UserMessages
{
    /// <summary>
    /// Converts an <see cref="ArgumentException"/> from a domain rule into "Field: message".
    /// </summary>
    /// <param name="exception">The exception thrown by the domain.</param>
    /// <returns>For example "Name: The value cannot be an empty string."</returns>
    /// <remarks>
    /// <see cref="ArgumentException.Message"/> ends with " (Parameter 'name')";
    /// that technical part is removed and the parameter name is used as the field label.
    /// </remarks>
    public static string From(ArgumentException exception)
    {
        var parameterName = exception.ParamName ?? "Input";
        var field = char.ToUpperInvariant(parameterName[0]) + parameterName[1..];
        var message = exception.Message.Replace($" (Parameter '{exception.ParamName}')", string.Empty, StringComparison.Ordinal);
        return $"{field}: {message}";
    }
}
