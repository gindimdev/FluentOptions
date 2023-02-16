namespace GDD.FluentOptions.Internal;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

internal sealed class FluentOptionsHostedService : IHostedService
{
	internal sealed class Options
	{
		internal IReadOnlySet<Type> IOptionsTypes => _ioptionsTypes;

		private readonly HashSet<Type> _ioptionsTypes = new();

		internal void AddOptions<TOptions, THelper>()
			where TOptions : class
			where THelper : FluentOptionsHelper<TOptions>
		{
			_ioptionsTypes.Add(typeof(IOptions<TOptions>));
		}
	}

	private readonly Options _options;
	private readonly IServiceProvider _services;

	public FluentOptionsHostedService(IOptions<Options> options, IServiceProvider services)
	{
		_options = options.Value;
		_services = services;
	}

	/// <inheritdoc />
	/// <exception cref="FluentOptionsStartupException"/>
	public Task StartAsync(CancellationToken cancellationToken)
	{
		var caughtExceptions = new List<FluentOptionsFailureException>();

		foreach (var iOptionsType in _options.IOptionsTypes)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var iOptions = (IOptions<object>)_services.GetService(iOptionsType)!;
			try
			{
				var _ = iOptions.Value;
			}
			catch (FluentOptionsFailureException failureException)
			{
				caughtExceptions.Add(failureException);
			}
		}

		if (caughtExceptions.Count > 0)
			throw new FluentOptionsStartupException(caughtExceptions);

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}