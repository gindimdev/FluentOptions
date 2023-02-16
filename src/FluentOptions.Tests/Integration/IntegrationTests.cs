namespace GDD.FluentOptions.Tests.Integration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public sealed class IntegrationTests
{
	private sealed class MockService
	{
		internal sealed class Options
		{
			internal static readonly ArgumentException FailConfigurationException = new(nameof(FailConfiguration));

			public required bool FailValidation { get; set; }
			public required bool FailConfiguration { get; set; }

			internal sealed class Helper : FluentOptionsHelper<Options>
			{
				public Helper()
				{
					Validate(static validator =>
					{
						validator.RuleFor(static o => o.FailValidation).NotEqual(true);
					});
					Configure(static options =>
					{
						if (options.FailConfiguration)
							throw FailConfigurationException;
					});
				}
			}
		}

		private readonly Options _options;

		public MockService(IOptions<Options> options)
		{
			_options = options.Value;
		}
	}

	private static readonly TimeSpan StartupWaitTime = TimeSpan.FromMilliseconds(100);

	[Fact]
	public void DI_ValidOptions_DoesNotThrow()
	{
		using var app = new HostBuilder().ConfigureServices(static services =>
		{
			services.AddFluentOptions<MockService.Options, MockService.Options.Helper>()
				.Configure(static options =>
				{
					options.FailValidation = false;
					options.FailConfiguration = false;
				});
			services.AddSingleton<MockService>();
		}).Build();

		Invoking(() => app.Services.GetRequiredService<MockService>()).Should().NotThrow();
	}
	[Fact]
	public void DI_ValidationFailure_Throws()
	{
		using var app = new HostBuilder().ConfigureServices(static services =>
		{
			services.AddFluentOptions<MockService.Options, MockService.Options.Helper>()
				.Configure(static options =>
				{
					options.FailValidation = true;
					options.FailConfiguration = false;
				});
			services.AddSingleton<MockService>();
		}).Build();

		var validationException =
			Invoking(() => app.Services.GetRequiredService<MockService>())
				.Should().Throw<FluentOptionsFailureException>()
				.Which.Should().BeOfType<FluentOptionsValidationException>().Which;
		using (new AssertionScope())
		{
			validationException.OptionsType.Should().Be<MockService.Options>();
			validationException.Failures.Should().ContainSingle()
				.Which.PropertyName.Should().Be(nameof(MockService.Options.FailValidation));
		}
	}
	[Fact]
	public void DI_ConfigurationFailure_Throws()
	{
		using var app = new HostBuilder().ConfigureServices(static services =>
		{
			services.AddFluentOptions<MockService.Options, MockService.Options.Helper>()
				.Configure(static options =>
				{
					options.FailValidation = false;
					options.FailConfiguration = true;
				});
			services.AddSingleton<MockService>();
		}).Build();

		var configurationException =
			Invoking(() => app.Services.GetRequiredService<MockService>())
				.Should().Throw<FluentOptionsFailureException>()
				.Which.Should().BeOfType<FluentOptionsConfigurationException>().Which;
		using (new AssertionScope())
		{
			configurationException.OptionsType.Should().Be<MockService.Options>();
			configurationException.Failure.Should().Be(MockService.Options.FailConfigurationException);
		}
	}

	[Fact]
	public async Task Startup_ValidOptions_DoesNotThrow()
	{
		using var app = new HostBuilder().ConfigureServices(static services =>
		{
			services.AddFluentOptions<MockService.Options, MockService.Options.Helper>()
				.Configure(static options =>
				{
					options.FailValidation = false;
					options.FailConfiguration = false;
				});
			services.AddFluentOptionsStartupCheck();
		}).Build();

		using var cts = new CancellationTokenSource(StartupWaitTime);
		await Invoking(
			async () => await app.RunAsync(cts.Token).ConfigureAwait(false)
		).Should().NotThrowAsync().ConfigureAwait(false);
	}
	[Fact]
	public async Task Startup_ValidationFailure_Throws()
	{
		using var app = new HostBuilder().ConfigureServices(static services =>
		{
			services.AddFluentOptions<MockService.Options, MockService.Options.Helper>()
				.Configure(static options =>
				{
					options.FailValidation = true;
					options.FailConfiguration = false;
				});
			services.AddFluentOptionsStartupCheck();
		}).Build();

		using var cts = new CancellationTokenSource(StartupWaitTime);
		var validationException = (
				await Invoking(
					async () => await app.RunAsync(cts.Token).ConfigureAwait(false)
				).Should().ThrowAsync<FluentOptionsStartupException>().ConfigureAwait(false)
			).Which.Failures.Should().ContainSingle()
			.Which.Should().BeOfType<FluentOptionsValidationException>().Which;
		using (new AssertionScope())
		{
			validationException.OptionsType.Should().Be<MockService.Options>();
			validationException.Failures.Should().ContainSingle()
				.Which.PropertyName.Should().Be(nameof(MockService.Options.FailValidation));
		}
	}
	[Fact]
	public async Task Startup_ConfigurationFailure_Throws()
	{
		using var app = new HostBuilder().ConfigureServices(static services =>
		{
			services.AddFluentOptions<MockService.Options, MockService.Options.Helper>()
				.Configure(static options =>
				{
					options.FailValidation = false;
					options.FailConfiguration = true;
				});
			services.AddFluentOptionsStartupCheck();
		}).Build();

		using var cts = new CancellationTokenSource(StartupWaitTime);
		var configurationException = (
				await Invoking(
					async () => await app.RunAsync(cts.Token).ConfigureAwait(false)
				).Should().ThrowAsync<FluentOptionsStartupException>().ConfigureAwait(false)
			).Which.Failures.Should().ContainSingle()
			.Which.Should().BeOfType<FluentOptionsConfigurationException>().Which;
		using (new AssertionScope())
		{
			configurationException.OptionsType.Should().Be<MockService.Options>();
			configurationException.Failure.Should().Be(MockService.Options.FailConfigurationException);
		}
	}
}