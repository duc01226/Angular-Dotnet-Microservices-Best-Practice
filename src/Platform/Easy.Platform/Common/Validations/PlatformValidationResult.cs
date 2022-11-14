using Easy.Platform.Common.Extensions;
using Easy.Platform.Common.Utils;
using FluentValidation.Results;

namespace Easy.Platform.Common.Validations;

public class PlatformValidationResult<TValue> : ValidationResult
{
    private List<PlatformValidationError> finalCombinedValidationErrors;

    public PlatformValidationResult()
    {
    }

    public PlatformValidationResult(
        TValue value,
        List<PlatformValidationError> failures,
        Func<PlatformValidationResult<TValue>, Exception> invalidException = null) : base(
        failures ?? new List<PlatformValidationError>())
    {
        Value = value;
        RootValidationErrors = failures ?? new List<PlatformValidationError>();
        InvalidException = invalidException;
    }

    public TValue Value { get; protected set; }
    public Func<PlatformValidationResult<TValue>, Exception> InvalidException { get; set; }
    public new List<PlatformValidationError> Errors => finalCombinedValidationErrors ??= FinalCombinedValidation().RootValidationErrors;
    public override bool IsValid => !Errors.Any();

    protected List<PlatformValidationError> RootValidationErrors { get; } = new List<PlatformValidationError>();
    protected bool IsRootValidationValid => !RootValidationErrors.Any();

    protected List<LogicalAndValidationsChainItem> LogicalAndValidationsChain { get; set; } = new List<LogicalAndValidationsChainItem>();

    /// <summary>
    ///     Dictionary map from LogicalAndValidationsChainItem Position to ExceptionCreatorFn
    /// </summary>
    protected Dictionary<int, Func<PlatformValidationResult<TValue>, Exception>> LogicalAndValidationsChainInvalidExceptions { get; } =
        new Dictionary<int, Func<PlatformValidationResult<TValue>, Exception>>();

    private PlatformValidationResult<TValue> FinalCombinedValidation()
    {
        return StandaloneRootValidation()
            .Pipe(selfValidation => IsRootValidationValid ? CombinedLogicalAndValidationsChain() : selfValidation);
    }

    private PlatformValidationResult<TValue> CombinedLogicalAndValidationsChain()
    {
        return LogicalAndValidationsChain
            .Aggregate(
                Valid(Value),
                (prevVal, nextValChainItem) => prevVal.IsValid ? nextValChainItem.ValidationFn(prevVal.Value) : prevVal,
                valResult => valResult);
    }

    private PlatformValidationResult<TValue> StandaloneRootValidation()
    {
        return RootValidationErrors.Any() ? Invalid(Value, RootValidationErrors.ToArray()) : Valid(Value);
    }

    public static implicit operator TValue(PlatformValidationResult<TValue> validation)
    {
        return validation.Value;
    }

    public static implicit operator bool(PlatformValidationResult<TValue> validation)
    {
        return validation.IsValid;
    }

    public static implicit operator string(PlatformValidationResult<TValue> validation)
    {
        return validation.ToString();
    }

    public static implicit operator PlatformValidationResult<TValue>(
        (TValue value, string error) invalidValidationInfo)
    {
        return Invalid(value: invalidValidationInfo.value, invalidValidationInfo.error);
    }

    public static implicit operator PlatformValidationResult<TValue>(
        (TValue value, List<string> errors) invalidValidationInfo)
    {
        return invalidValidationInfo.errors?.Any() == true
            ? Invalid(
                value: invalidValidationInfo.value,
                invalidValidationInfo.errors.Select(p => (PlatformValidationError)p).ToArray())
            : Valid(invalidValidationInfo.value);
    }

    public static implicit operator PlatformValidationResult<TValue>(
        (TValue value, List<PlatformValidationError> errors) invalidValidationInfo)
    {
        return invalidValidationInfo.errors?.Any() == true
            ? Invalid(value: invalidValidationInfo.value, invalidValidationInfo.errors.Select(p => p).ToArray())
            : Valid(invalidValidationInfo.value);
    }

    public static implicit operator PlatformValidationResult<TValue>(
        PlatformValidationResult validation)
    {
        return new PlatformValidationResult<TValue>((TValue)validation.Value, validation.Errors);
    }

    public static implicit operator PlatformValidationResult(
        PlatformValidationResult<TValue> validation)
    {
        return new PlatformValidationResult(validation.Value, validation.Errors);
    }

    /// <summary>
    ///     Return a valid validation result.
    /// </summary>
    /// <returns>A valid validation result.</returns>
    internal static PlatformValidationResult<TValue> Valid(TValue value = default)
    {
        return new PlatformValidationResult<TValue>(value, null);
    }

    /// <summary>
    ///     Return a invalid validation result.
    /// </summary>
    /// <param name="value">The validation target object.</param>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A invalid validation result.</returns>
    internal static PlatformValidationResult<TValue> Invalid(
        TValue value,
        params PlatformValidationError[] errors)
    {
        return errors.Any()
            ? new PlatformValidationResult<TValue>(value, errors.ToList())
            : new PlatformValidationResult<TValue>(
                value,
                Util.ListBuilder.New<PlatformValidationError>("Invalid!"));
    }

    /// <summary>
    ///     Return a valid validation result if the condition is true, otherwise return a invalid validation with errors.
    /// </summary>
    /// <param name="value">The validation target object.</param>
    /// <param name="must">The valid condition.</param>
    /// <param name="errors">The errors if the valid condition is false.</param>
    /// <returns>A validation result.</returns>
    public static PlatformValidationResult<TValue> Validate(
        TValue value,
        bool must,
        params PlatformValidationError[] errors)
    {
        return Validate(value, () => must, errors);
    }

    /// <inheritdoc cref="Validate(TValue,bool,Easy.Platform.Common.Validations.PlatformValidationError[])" />
    public static PlatformValidationResult<TValue> Validate(
        TValue value,
        Func<bool> must,
        params PlatformValidationError[] errors)
    {
        return must() ? Valid(value) : Invalid(value, errors);
    }

    /// <summary>
    /// Combine all validation but fail fast. Ex: [Val1, Val2].Combine() = Val1 && Val2
    /// </summary>
    public static PlatformValidationResult<TValue> Combine(
        params Func<PlatformValidationResult<TValue>>[] validations)
    {
        return validations.IsEmpty()
            ? Valid()
            : validations.Aggregate((prevVal, nextVal) => () => prevVal().Then(() => nextVal()))();
    }

    /// <inheritdoc cref="Combine(System.Func{Easy.Platform.Common.Validations.PlatformValidationResult{TValue}}[])" />
    public static PlatformValidationResult<TValue> Combine(
        params PlatformValidationResult<TValue>[] validations)
    {
        return Combine(validations.Select(p => (Func<PlatformValidationResult<TValue>>)(() => p)).ToArray());
    }

    /// <summary>
    /// Aggregate all validations, collect all validations errors. Ex: [Val1, Val2].Combine() = Val1 & Val2 (Mean that
    /// execute both Val1 and Val2, then harvest return all errors from both all validations in list)
    /// </summary>
    public static PlatformValidationResult<TValue> Aggregate(
        params PlatformValidationResult<TValue>[] validations)
    {
        return validations.IsEmpty()
            ? Valid()
            : validations.Aggregate(
                (prevVal, nextVal) => new PlatformValidationResult<TValue>(nextVal.Value, prevVal.Errors.Concat(nextVal.Errors).ToList()));
    }

    /// <inheritdoc cref="Aggregate(Easy.Platform.Common.Validations.PlatformValidationResult{TValue}[])" />
    public static PlatformValidationResult<TValue> Aggregate(
        TValue value,
        params (bool, PlatformValidationError)[] validations)
    {
        return Aggregate(
            validations
                .Select(validationInfo => Validate(value, must: validationInfo.Item1, errors: validationInfo.Item2))
                .ToArray());
    }

    /// <inheritdoc cref="Aggregate(Easy.Platform.Common.Validations.PlatformValidationResult{TValue}[])" />
    public static PlatformValidationResult<TValue> Aggregate(
        params Func<PlatformValidationResult<TValue>>[] validations)
    {
        return Aggregate(validations.Select(p => p()).ToArray());
    }

    public string ErrorsMsg()
    {
        return Errors?.Aggregate(
            string.Empty,
            (currentMsg, error) => $"{(currentMsg == string.Empty ? string.Empty : ". ")}{error}.");
    }

    public override string ToString()
    {
        return ErrorsMsg();
    }

    public PlatformValidationResult<T> Then<T>(
        Func<PlatformValidationResult<T>> nextVal)
    {
        return Match(
            valid: value => nextVal(),
            invalid: err => Of<T>(default));
    }

    public PlatformValidationResult<T> Then<T>(
        Func<TValue, PlatformValidationResult<T>> nextVal)
    {
        return Match(
            valid: value => nextVal(Value),
            invalid: err => Of<T>(default));
    }

    public PlatformValidationResult<T> Then<T>(
        Func<TValue, T> next)
    {
        return Match(
            valid: value => new PlatformValidationResult<T>(next(value), null),
            invalid: err => Of<T>(default));
    }

    public PlatformValidationResult<T> Match<T>(
        Func<TValue, PlatformValidationResult<T>> valid,
        Func<IEnumerable<PlatformValidationError>, PlatformValidationResult<T>> invalid)
    {
        return IsValid ? valid(Value) : invalid(Errors);
    }

    public PlatformValidationResult<TValue> And(Func<TValue, PlatformValidationResult<TValue>> nextValidation)
    {
        LogicalAndValidationsChain.Add(
            new LogicalAndValidationsChainItem
            {
                ValidationFn = nextValidation,
                Position = LogicalAndValidationsChain.Count
            });
        return this;
    }

    public PlatformValidationResult<TValue> And(PlatformValidationResult<TValue> nextValidation)
    {
        return And(value => nextValidation);
    }

    public PlatformValidationResult<TValue> And(
        Func<TValue, bool> must,
        params PlatformValidationError[] errors)
    {
        return And(() => Validate(value: Value, () => must(Value), errors));
    }

    public PlatformValidationResult<TValue> And(Func<PlatformValidationResult<TValue>> nextValidation)
    {
        return And(value => nextValidation());
    }

    public async Task<PlatformValidationResult<TValue>> And(
        Task<PlatformValidationResult<TValue>> nextValidation)
    {
        return !IsValid ? this : await nextValidation;
    }

    public async Task<PlatformValidationResult<TValue>> And(
        Func<TValue, Task<PlatformValidationResult<TValue>>> nextValidation)
    {
        return !IsValid ? this : await nextValidation(Value);
    }

    public PlatformValidationResult<TValue> Or(PlatformValidationResult<TValue> nextValidation)
    {
        return IsValid ? this : nextValidation;
    }

    public PlatformValidationResult<TValue> Or(Func<PlatformValidationResult<TValue>> nextValidation)
    {
        return IsValid ? this : nextValidation();
    }

    public async Task<PlatformValidationResult<TValue>> Or(
        Task<PlatformValidationResult<TValue>> nextValidation)
    {
        return IsValid ? this : await nextValidation;
    }

    public async Task<PlatformValidationResult<TValue>> Or(
        Func<Task<PlatformValidationResult<TValue>>> nextValidation)
    {
        return IsValid ? this : await nextValidation();
    }

    public TValue EnsureValid(Func<PlatformValidationResult<TValue>, Exception> invalidException = null)
    {
        if (Errors.Any())
            throw invalidException != null
                ? invalidException(this)
                : InvalidException != null
                    ? InvalidException(this)
                    : new Exception(message: ErrorsMsg());

        if (LogicalAndValidationsChain.Any())
            return LogicalAndValidationsChain
                .Aggregate(
                    Valid(Value),
                    (prevValResult, nextValChainItem) =>
                        prevValResult.IsValid ? nextValChainItem.ValidationFn(prevValResult.Value) : prevValResult,
                    valResult => valResult)
                .EnsureValid();

        return Value;
    }

    public PlatformValidationResult<T> Of<T>(T value)
    {
        return new PlatformValidationResult<T>(
            value,
            Errors,
            InvalidException != null ? val => InvalidException(this) : null);
    }

    public PlatformValidationResult<T> Of<T>()
    {
        return new PlatformValidationResult<T>(
            Value.Cast<T>(),
            Errors,
            InvalidException != null ? val => InvalidException(this) : null);
    }

    public PlatformValidationResult<TValue> WithInvalidException(Func<PlatformValidationResult<TValue>, Exception> invalidException)
    {
        if (!LogicalAndValidationsChain.Any())
            InvalidException = invalidException;
        else
            LogicalAndValidationsChain
                .Where(
                    andValidationsChainItem => !LogicalAndValidationsChainInvalidExceptions.ContainsKey(andValidationsChainItem.Position))
                .Ensure(
                    notSetInvalidExceptionAndValidations => notSetInvalidExceptionAndValidations.Any(),
                    "All InvalidException has been set")
                .ForEach(
                    notSetInvalidExceptionAndValidation =>
                        LogicalAndValidationsChainInvalidExceptions.Add(notSetInvalidExceptionAndValidation.Position, invalidException));

        return this;
    }

    /// <summary>
    ///     Do validation all conditions in AndConditions Chain when .And().And() and collect all errors
    ///     <br />
    ///     "andValidationChainItem => Util.TaskRunner.CatchException(" Explain:
    ///     Because the and chain could depend on previous chain, so do harvest validation errors could throw errors on some
    ///     depended validation, so we catch and ignore the depended chain item
    ///     try to get all available interdependent/not depend on prev validation item chain validations
    /// </summary>
    public List<PlatformValidationError> AggregateErrors()
    {
        return Util.ListBuilder.New(StandaloneRootValidation().Errors.ToArray())
            .Concat(
                LogicalAndValidationsChain.SelectMany(
                    andValidationChainItem => Util.TaskRunner.CatchException(
                        () => andValidationChainItem.ValidationFn(Value).Errors,
                        new List<PlatformValidationError>())))
            .ToList();
    }

    public class LogicalAndValidationsChainItem
    {
        public Func<TValue, PlatformValidationResult<TValue>> ValidationFn { get; set; }
        public int Position { get; set; }
    }
}

public class PlatformValidationResult : PlatformValidationResult<object>
{
    public PlatformValidationResult(
        object value,
        List<PlatformValidationError> failures,
        Func<PlatformValidationResult<object>, Exception> invalidException = null) : base(value: value, failures, invalidException)
    {
    }

    public static implicit operator PlatformValidationResult(string error)
    {
        return Invalid<object>(null, error);
    }

    public static implicit operator PlatformValidationResult(List<string> errors)
    {
        return Invalid<object>(null, errors?.Select(p => (PlatformValidationError)p).ToArray());
    }

    public static implicit operator PlatformValidationResult(List<PlatformValidationError> errors)
    {
        return Invalid<object>(null, errors.ToArray());
    }

    /// <summary>
    ///     Return a valid validation result.
    /// </summary>
    /// <returns>A valid validation result.</returns>
    public static PlatformValidationResult<TValue> Valid<TValue>(TValue value = default)
    {
        return new PlatformValidationResult<TValue>(value, null);
    }

    /// <summary>
    ///     Return a invalid validation result.
    /// </summary>
    /// <param name="value">The validation target object.</param>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A invalid validation result.</returns>
    public static PlatformValidationResult<TValue> Invalid<TValue>(
        TValue value,
        params PlatformValidationError[] errors)
    {
        return errors?.Any() == true
            ? new PlatformValidationResult<TValue>(value, errors.ToList())
            : new PlatformValidationResult<TValue>(
                value,
                Util.ListBuilder.New<PlatformValidationError>("Invalid!"));
    }

    /// <summary>
    ///     Return a valid validation result if the condition is true, otherwise return a invalid validation with errors.
    /// </summary>
    /// <param name="must">The valid condition.</param>
    /// <param name="errors">The errors if the valid condition is false.</param>
    /// <returns>A validation result.</returns>
    public static PlatformValidationResult<TValue> Validate<TValue>(
        TValue value,
        bool must,
        params PlatformValidationError[] errors)
    {
        return must ? Valid(value: value) : Invalid(value: value, errors);
    }

    /// <inheritdoc cref="Validate(bool,Easy.Platform.Common.Validations.PlatformValidationError[])" />
    public static PlatformValidationResult<TValue> Validate<TValue>(
        TValue value,
        Func<bool> validConditionFunc,
        params PlatformValidationError[] errors)
    {
        return Validate(value, validConditionFunc(), errors);
    }

    /// <inheritdoc cref="PlatformValidationResult{TValue}.Combine(Func{PlatformValidationResult{TValue}}[])"/>
    public static PlatformValidationResult Combine(
        params Func<PlatformValidationResult>[] validations)
    {
        return validations.Aggregate(
            seed: Valid(validations.First()().Value),
            (acc, validator) => acc.Then(() => validator()));
    }

    /// <inheritdoc cref="PlatformValidationResult{TValue}.Aggregate(PlatformValidationResult{TValue}[])"/>
    public static PlatformValidationResult Aggregate(
        params PlatformValidationResult[] validations)
    {
        return validations.IsEmpty()
            ? Valid()
            : validations.Aggregate(
                (prevVal, nextVal) => new PlatformValidationResult(nextVal.Value, prevVal.Errors.Concat(nextVal.Errors).ToList()));
    }

    public TValue EnsureValid<TValue>(Func<PlatformValidationResult, Exception> invalidException = null)
    {
        return EnsureValid(invalidException != null ? _ => invalidException(this) : null).Cast<TValue>();
    }
}