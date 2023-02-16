namespace GDD.FluentOptions;

using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.Options;

public abstract class FluentOptionsHelper<TOptions> : IPostConfigureOptions<TOptions> where TOptions : class
{
	private readonly IList<object> _actions = new List<object>();

	protected void Validate(Action<InlineValidator<TOptions>> setupRules)
	{
		var inlineValidator = new InlineValidator<TOptions>();
		setupRules(inlineValidator);
		_actions.Add(inlineValidator);
	}
	protected void Configure(Action<TOptions> configure)
	{
		_actions.Add(configure);
	}

	/// <inheritdoc/>
	/// <exception cref="FluentOptionsFailureException"/>
	public void PostConfigure(string? name, TOptions options)
	{
		foreach (var action in _actions)
			ExecuteAction(action, options, name);
	}
	/// <exception cref="FluentOptionsValidationException"/>
	/// <exception cref="FluentOptionsConfigurationException"/>
	private static void ExecuteAction(object action, TOptions options, string? optionsName)
	{
		switch (action)
		{
			case InlineValidator<TOptions> validator:
				var result = validator.Validate(options);
				if (!result.IsValid)
					throw new FluentOptionsValidationException(typeof(TOptions), optionsName, result.Errors);
				break;
			case Action<TOptions> configure:
				try
				{
					configure(options);
				}
				catch (Exception exception)
				{
					throw new FluentOptionsConfigurationException(typeof(TOptions), optionsName, exception);
				}
				break;
			default: throw new UnreachableException();
		}
	}
}