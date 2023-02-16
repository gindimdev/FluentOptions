namespace GDD.FluentOptions;

using FluentValidation.Results;

// Implement standard exception constructors - Non-public constructors
#pragma warning disable CA1032

/// <inheritdoc />
/// <summary>Base exception for all <see cref="GDD.FluentOptions"/> exceptions</summary>
public abstract class FluentOptionsException : Exception
{
	protected internal FluentOptionsException(string message, Exception? innerException = null) : base(message, innerException) { }
}

public sealed class FluentOptionsStartupException : FluentOptionsException
{
	public IReadOnlyCollection<FluentOptionsFailureException> Failures { get; }

	internal FluentOptionsStartupException(IReadOnlyCollection<FluentOptionsFailureException> failures) : base($"{nameof(FluentOptions)} startup options failure")
	{
		Failures = failures;
	}
}

public abstract class FluentOptionsFailureException : FluentOptionsException
{
	public Type OptionsType { get; }
	public string? OptionsName { get; }

	protected internal FluentOptionsFailureException(Type optionsType, string? optionsName, string message, Exception? innerException = null) : base(message, innerException)
	{
		OptionsType = optionsType;
		OptionsName = optionsName;
	}
}

public sealed class FluentOptionsValidationException : FluentOptionsFailureException
{
	public IReadOnlyList<ValidationFailure> Failures { get; }

	internal FluentOptionsValidationException(Type optionsType, string? optionsName, IReadOnlyList<ValidationFailure> failures) : base(optionsType, optionsName, "Options validation failure")
	{
		Failures = failures;
	}
}

public sealed class FluentOptionsConfigurationException : FluentOptionsFailureException
{
	public Exception Failure => InnerException!;

	internal FluentOptionsConfigurationException(Type optionsType, string? optionsName, Exception innerException) : base(optionsType, optionsName, "Options configuration failure", innerException) { }
}