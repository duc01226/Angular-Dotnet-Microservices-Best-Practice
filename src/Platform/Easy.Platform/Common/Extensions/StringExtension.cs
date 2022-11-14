using System.Diagnostics.Contracts;
using System.Text;
using System.Text.RegularExpressions;
using Easy.Platform.Common.JsonSerialization;
using Easy.Platform.Common.Validations;

namespace Easy.Platform.Common.Extensions;

public static class StringExtension
{
    public static string TakeTop(this string strValue, int takeMaxLength)
    {
        return strValue.Length >= takeMaxLength ? strValue.Substring(0, takeMaxLength) : strValue;
    }

    public static string EnsureNotNullOrWhiteSpace(this string target, Func<Exception> exception)
    {
        return target.Ensure(must: target => !string.IsNullOrWhiteSpace(target), exception);
    }

    public static string EnsureNotNullOrEmpty(this string target, Func<Exception> exception)
    {
        return target.Ensure(must: target => !string.IsNullOrEmpty(target), exception);
    }

    public static T ParseToSerializableType<T>(this string strValue)
    {
        return strValue != null ? PlatformJsonSerializer.Deserialize<T>(PlatformJsonSerializer.Serialize(strValue)) : default;
    }

    public static string SliceFromRight(this string strValue, int fromIndex, int toIndex = 0)
    {
        return strValue.Substring(toIndex, strValue.Length - fromIndex);
    }

    public static bool IsNullOrEmpty(this string strValue)
    {
        return string.IsNullOrEmpty(strValue);
    }

    public static bool IsNullOrWhiteSpace(this string strValue)
    {
        return string.IsNullOrWhiteSpace(strValue);
    }

    [Pure]
    public static string RemoveSpecialCharactersUri(this string source, string replace = "")
    {
        return Regex.Replace(source, @"[^0-9a-zA-Z\._()-\/]+", replace);
    }

    public static T ParseToEnum<T>(this string enumStringValue) where T : Enum
    {
        return (T)Enum.Parse(typeof(T), enumStringValue);
    }

    public static PlatformValidationResult<T> TryParseToEnum<T>(this string enumStringValue) where T : Enum
    {
        try
        {
            return PlatformValidationResult<T>.Valid((T)Enum.Parse(typeof(T), enumStringValue));
        }
        catch
        {
            return PlatformValidationResult<T>.Invalid(default, $"Can't parse '{enumStringValue}' to enum {nameof(T)}");
        }
    }

    public static string Duplicate(this string duplicateStr, int numberOfDuplicateTimes)
    {
        var strBuilder = new StringBuilder();

        for (var i = 0; i <= numberOfDuplicateTimes; i++) strBuilder.Append(duplicateStr);

        return strBuilder.ToString();
    }
}