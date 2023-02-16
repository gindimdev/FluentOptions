namespace GDD.FluentOptions.Tests.Unit.Internal;

using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public sealed class FluentOptionsHostedServiceTests
{
	private static readonly TimeSpan WaitTime = TimeSpan.FromMilliseconds(100);

	public sealed class MockOptions1
	{
		internal sealed class Helper : FluentOptionsHelper<MockOptions1> { }
	}
	public sealed class MockOptions2
	{
		public required string MockProperty { get; set; }
		internal sealed class Helper : FluentOptionsHelper<MockOptions2> { }
	}
	public sealed class MockOptions3
	{
		internal sealed class Helper : FluentOptionsHelper<MockOptions3> { }
	}

	[Fact]
	public async Task StartAsync_InvokesIOptionsValues()
	{
		var options = new FluentOptionsHostedService.Options();
		options.AddOptions<MockOptions1, MockOptions1.Helper>();
		options.AddOptions<MockOptions2, MockOptions2.Helper>();
		options.AddOptions<MockOptions3, MockOptions3.Helper>();

		var mockIOptions1 = new Mock<IOptions<MockOptions1>>();
		var mockIOptions2 = new Mock<IOptions<MockOptions2>>();
		var mockIOptions3 = new Mock<IOptions<MockOptions3>>();

		var services = new Mock<IServiceProvider>();
		services.Setup(static s => s.GetService(typeof(IOptions<MockOptions1>))).Returns(mockIOptions1.Object);
		services.Setup(static s => s.GetService(typeof(IOptions<MockOptions2>))).Returns(mockIOptions2.Object);
		services.Setup(static s => s.GetService(typeof(IOptions<MockOptions3>))).Returns(mockIOptions3.Object);

		var service = new FluentOptionsHostedService(Options.Create(options), services.Object);
		using var cts = new CancellationTokenSource(WaitTime);
		await service.StartAsync(cts.Token).ConfigureAwait(false);

		services.Verify(static s => s.GetService(typeof(IOptions<MockOptions1>)), Times.Once);
		services.Verify(static s => s.GetService(typeof(IOptions<MockOptions2>)), Times.Once);
		services.Verify(static s => s.GetService(typeof(IOptions<MockOptions3>)), Times.Once);
		mockIOptions1.Verify(static i => i.Value, Times.Once);
		mockIOptions2.Verify(static i => i.Value, Times.Once);
		mockIOptions3.Verify(static i => i.Value, Times.Once);
	}

	[Fact]
	public async Task StartAsync_OptionsFailures_AggregatesExceptions()
	{
		var options = new FluentOptionsHostedService.Options();
		options.AddOptions<MockOptions1, MockOptions1.Helper>();
		options.AddOptions<MockOptions2, MockOptions2.Helper>();
		options.AddOptions<MockOptions3, MockOptions3.Helper>();

		var mockIOptions1 = new Mock<IOptions<MockOptions1>>();
		var mockIOptions2 = new Mock<IOptions<MockOptions2>>();
		var mockIOptions3 = new Mock<IOptions<MockOptions3>>();

		FluentOptionsFailureException options2Failure = new FluentOptionsValidationException(
			typeof(MockOptions2),
			Options.DefaultName,
			new[] { new ValidationFailure(nameof(MockOptions2.MockProperty), "Test") }
		);
		mockIOptions2.Setup(static i => i.Value).Throws(options2Failure);

		FluentOptionsFailureException options3Failure = new FluentOptionsConfigurationException(
			typeof(MockOptions3),
			Options.DefaultName,
			new ArgumentException("Test")
		);
		mockIOptions3.Setup(static i => i.Value).Throws(options3Failure);

		var services = new Mock<IServiceProvider>();
		services.Setup(static s => s.GetService(typeof(IOptions<MockOptions1>))).Returns(mockIOptions1.Object);
		services.Setup(static s => s.GetService(typeof(IOptions<MockOptions2>))).Returns(mockIOptions2.Object);
		services.Setup(static s => s.GetService(typeof(IOptions<MockOptions3>))).Returns(mockIOptions3.Object);

		var service = new FluentOptionsHostedService(Options.Create(options), services.Object);
		using var cts = new CancellationTokenSource(WaitTime);

		var startupException = (
			await Invoking(
				async () => await service.StartAsync(cts.Token).ConfigureAwait(false)
			).Should().ThrowAsync<FluentOptionsStartupException>().ConfigureAwait(false)
		).Which;
		startupException.Failures.Should().BeEquivalentTo(new[] { options2Failure, options3Failure });

		services.Verify(static s => s.GetService(typeof(IOptions<MockOptions1>)), Times.Once);
		services.Verify(static s => s.GetService(typeof(IOptions<MockOptions2>)), Times.Once);
		services.Verify(static s => s.GetService(typeof(IOptions<MockOptions3>)), Times.Once);
		mockIOptions1.Verify(static i => i.Value, Times.Once);
		mockIOptions2.Verify(static i => i.Value, Times.Once);
		mockIOptions3.Verify(static i => i.Value, Times.Once);
	}

	[Fact]
	public async Task StartAsync_CancellationRequested_Cancels()
	{
		var options = new FluentOptionsHostedService.Options();
		options.AddOptions<MockOptions1, MockOptions1.Helper>();
		options.AddOptions<MockOptions2, MockOptions2.Helper>();
		options.AddOptions<MockOptions3, MockOptions3.Helper>();

		var mockIOptions1 = new Mock<IOptions<MockOptions1>>();
		var mockIOptions2 = new Mock<IOptions<MockOptions2>>();
		var mockIOptions3 = new Mock<IOptions<MockOptions3>>();

		using var cts = new CancellationTokenSource();
		mockIOptions2.Setup(static i => i.Value).Callback(() => cts.Cancel());

		var services = new Mock<IServiceProvider>();
		services.Setup(static s => s.GetService(typeof(IOptions<MockOptions1>))).Returns(mockIOptions1.Object);
		services.Setup(static s => s.GetService(typeof(IOptions<MockOptions2>))).Returns(mockIOptions2.Object);
		services.Setup(static s => s.GetService(typeof(IOptions<MockOptions3>))).Returns(mockIOptions3.Object);

		var service = new FluentOptionsHostedService(Options.Create(options), services.Object);
		await Invoking(
			async () => await service.StartAsync(cts.Token).ConfigureAwait(false)
		).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

		services.Verify(static s => s.GetService(typeof(IOptions<MockOptions1>)), Times.Once);
		services.Verify(static s => s.GetService(typeof(IOptions<MockOptions2>)), Times.Once);
		services.Verify(static s => s.GetService(typeof(IOptions<MockOptions3>)), Times.Never);
		mockIOptions1.Verify(static i => i.Value, Times.Once);
		mockIOptions2.Verify(static i => i.Value, Times.Once);
		mockIOptions3.Verify(static i => i.Value, Times.Never);
	}

	[Fact]
	public void StopAsync_DoesNothing()
	{
		using var cts = new CancellationTokenSource(WaitTime);
		var service = new FluentOptionsHostedService(
			Options.Create(new FluentOptionsHostedService.Options()),
			new ServiceCollection().BuildServiceProvider()
		);
		service.StopAsync(cts.Token).Should().Be(Task.CompletedTask);
	}
}