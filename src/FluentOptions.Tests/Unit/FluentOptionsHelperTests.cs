namespace GDD.FluentOptions.Tests.Unit;

using Microsoft.Extensions.Options;

public sealed class FluentOptionsHelperTests
{
	private sealed class MockOptions
	{
		internal static readonly ArgumentException Fail2Exception = new(nameof(Fail2Configuration));
		internal static readonly ArgumentException Fail4Exception = new(nameof(Fail4Configuration));

		public required bool Fail1Validation { get; init; }
		public required bool Fail2Configuration { get; init; }
		public required bool Fail3Validation { get; init; }
		public required bool Fail4Configuration { get; init; }

		private readonly List<int> _callOrder = new();
		public IReadOnlyList<int> CallOrder => _callOrder;

		internal sealed class Helper : FluentOptionsHelper<MockOptions>
		{
			public Helper()
			{
				Validate(static validator =>
				{
					validator.RuleFor(static options => options.Fail1Validation)
						.Must(static (options, _) =>
						{
							options._callOrder.Add(1);
							return true;
						})
						.NotEqual(true);
				});
				Configure(static options =>
				{
					options._callOrder.Add(2);
					if (options.Fail2Configuration)
						throw Fail2Exception;
				});
				Validate(static validator =>
				{
					validator.RuleFor(static options => options.Fail3Validation)
						.Must(static (options, _) =>
						{
							options._callOrder.Add(3);
							return true;
						})
						.NotEqual(true);
				});
				Configure(static options =>
				{
					options._callOrder.Add(4);
					if (options.Fail4Configuration)
						throw Fail4Exception;
				});
			}
		}
	}

	[Fact]
	public void PostConfigure_ValidOptions_DoesNotThrowAndRespectsCallOrder()
	{
		var optionsName = Options.DefaultName;
		var options = new MockOptions
		{
			Fail1Validation = false,
			Fail2Configuration = false,
			Fail3Validation = false,
			Fail4Configuration = false
		};
		Invoking(() => new MockOptions.Helper().PostConfigure(optionsName, options)).Should().NotThrow();
		options.CallOrder.Should().BeEquivalentTo(new[] { 1, 2, 3, 4 }, static o => o.WithStrictOrdering());
	}

	[Fact]
	public void PostConfigure_Fail1Validation_ThrowsAndDoesNotCallRemaining()
	{
		var optionsName = Options.DefaultName;
		var options = new MockOptions
		{
			Fail1Validation = true,
			Fail2Configuration = false,
			Fail3Validation = false,
			Fail4Configuration = false
		};
		var validationException =
			Invoking(() => new MockOptions.Helper().PostConfigure(optionsName, options))
				.Should().Throw<FluentOptionsFailureException>()
				.Which.Should().BeOfType<FluentOptionsValidationException>().Which;
		using (new AssertionScope())
		{
			validationException.OptionsType.Should().Be<MockOptions>();
			validationException.OptionsName.Should().Be(optionsName);
			validationException.Failures.Should().ContainSingle()
				.Which.PropertyName.Should().Be(nameof(MockOptions.Fail1Validation));
		}
		options.CallOrder.Should().BeEquivalentTo(new[] { 1 }, static o => o.WithStrictOrdering());
	}

	[Fact]
	public void PostConfigure_Fail2Configuration_ThrowsAndDoesNotCallRemaining()
	{
		var optionsName = Options.DefaultName;
		var options = new MockOptions
		{
			Fail1Validation = false,
			Fail2Configuration = true,
			Fail3Validation = false,
			Fail4Configuration = false
		};
		var configurationException =
			Invoking(() => new MockOptions.Helper().PostConfigure(optionsName, options))
				.Should().Throw<FluentOptionsFailureException>()
				.Which.Should().BeOfType<FluentOptionsConfigurationException>().Which;
		using (new AssertionScope())
		using (new AssertionScope())
		{
			configurationException.OptionsType.Should().Be<MockOptions>();
			configurationException.OptionsName.Should().Be(optionsName);
			configurationException.Failure.Should().Be(MockOptions.Fail2Exception);
		}
		options.CallOrder.Should().BeEquivalentTo(new[] { 1, 2 }, static o => o.WithStrictOrdering());
	}

	[Fact]
	public void PostConfigure_Fail3Validation_ThrowsAndDoesNotCallRemaining()
	{
		var optionsName = Options.DefaultName;
		var options = new MockOptions
		{
			Fail1Validation = false,
			Fail2Configuration = false,
			Fail3Validation = true,
			Fail4Configuration = false
		};
		var validationException =
			Invoking(() => new MockOptions.Helper().PostConfigure(optionsName, options))
				.Should().Throw<FluentOptionsFailureException>()
				.Which.Should().BeOfType<FluentOptionsValidationException>().Which;
		using (new AssertionScope())
		{
			validationException.OptionsType.Should().Be<MockOptions>();
			validationException.OptionsName.Should().Be(optionsName);
			validationException.Failures.Should().ContainSingle()
				.Which.PropertyName.Should().Be(nameof(MockOptions.Fail3Validation));
		}
		options.CallOrder.Should().BeEquivalentTo(new[] { 1, 2, 3 }, static o => o.WithStrictOrdering());
	}

	[Fact]
	public void PostConfigure_Fail4Configuration_Throws()
	{
		var optionsName = Options.DefaultName;
		var options = new MockOptions
		{
			Fail1Validation = false,
			Fail2Configuration = false,
			Fail3Validation = false,
			Fail4Configuration = true
		};
		var configurationException =
			Invoking(() => new MockOptions.Helper().PostConfigure(optionsName, options))
				.Should().Throw<FluentOptionsFailureException>()
				.Which.Should().BeOfType<FluentOptionsConfigurationException>().Which;
		using (new AssertionScope())
		{
			configurationException.OptionsType.Should().Be<MockOptions>();
			configurationException.OptionsName.Should().Be(optionsName);
			configurationException.Failure.Should().Be(MockOptions.Fail4Exception);
		}
		options.CallOrder.Should().BeEquivalentTo(new[] { 1, 2, 3, 4 }, static o => o.WithStrictOrdering());
	}
}