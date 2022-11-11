namespace Easy.Platform.Common.Validations.Extensions;

public static class PlatformCombineValidationsExtension
{
    /// <summary>
    /// Combine all validation but fail fast. Ex: [Val1, Val2].Combine() = Val1 && Val2
    /// </summary>
    public static PlatformValidationResult<TValue> Combine<TValue>(
        this IEnumerable<Func<PlatformValidationResult<TValue>>> validations)
    {
        return PlatformValidationResult<TValue>.Combine(validations.ToArray());
    }

    /// <summary>
    /// Combine all validation but fail fast. Ex: [Val1, Val2].Combine() = Val1 && Val2
    /// </summary>
    public static PlatformValidationResult<TValue> Combine<TValue>(
        this IEnumerable<PlatformValidationResult<TValue>> validations)
    {
        return PlatformValidationResult<TValue>.Combine(validations.ToArray());
    }

    /// <summary>
    /// Aggregate all validations, collect all validations errors. Ex: [Val1, Val2].Combine() = Val1 & Val2 (Mean that
    /// execute both Val1 and Val2, then harvest return all errors from both all validations in list)
    /// </summary>
    public static PlatformValidationResult<TValue> Aggregate<TValue>(
        this IEnumerable<PlatformValidationResult<TValue>> validations)
    {
        return PlatformValidationResult<TValue>.Aggregate(validations.ToArray());
    }

    /// <inheritdoc cref="Aggregate{TValue}(IEnumerable{PlatformValidationResult{TValue}})" />
    public static PlatformValidationResult<TValue> Aggregate<TValue>(
        this TValue value,
        params (bool, PlatformValidationError)[] validateConditions)
    {
        return PlatformValidationResult<TValue>.Aggregate(value, validateConditions);
    }

    /// <inheritdoc cref="Aggregate{TValue}(IEnumerable{PlatformValidationResult{TValue}})" />
    public static PlatformValidationResult<TValue> Aggregate<TValue>(
        this IEnumerable<Func<PlatformValidationResult<TValue>>> validations)
    {
        return PlatformValidationResult<TValue>.Aggregate(validations.ToArray());
    }
}
