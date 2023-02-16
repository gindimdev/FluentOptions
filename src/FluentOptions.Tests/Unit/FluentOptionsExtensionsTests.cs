namespace GDD.FluentOptions.Tests.Unit;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

public sealed class FluentOptionsExtensionsTests
{
	private sealed class MockOptions
	{
		public required bool MockProperty { get; set; }

		internal sealed class Helper : FluentOptionsHelper<MockOptions> { }
	}

	[Fact]
	public void IServiceCollection_AddFluentOptions()
	{
		IServiceCollection services = new ServiceCollection();
		services
			.AddFluentOptions<MockOptions, MockOptions.Helper>()
			.Configure(static o => o.MockProperty = true);

		services.Should().Contain(static descriptor =>
			descriptor.ServiceType == typeof(IOptions<>)
		);
		services.Should().Contain(static descriptor =>
			descriptor.Lifetime == ServiceLifetime.Singleton &&
			descriptor.ServiceType == typeof(IPostConfigureOptions<MockOptions>) &&
			descriptor.ImplementationType == typeof(MockOptions.Helper)
		);
		services.Should().NotContain(static descriptor =>
			descriptor.ImplementationType == typeof(FluentOptionsHostedService)
		);

		var provider = services.BuildServiceProvider();
		provider.GetService<IOptions<MockOptions>>()!.Value.MockProperty.Should().BeTrue();
		provider.GetService<IOptions<FluentOptionsHostedService.Options>>()!.Value
			.IOptionsTypes.Should().ContainSingle()
			.Which.Should().Be<IOptions<MockOptions>>();
	}

	[Fact]
	public void IServiceCollection_AddFluentOptionsStartupCheck()
	{
		IServiceCollection services = new ServiceCollection();
		services.AddFluentOptionsStartupCheck();
		services.Should().Contain(static descriptor =>
			descriptor.Lifetime == ServiceLifetime.Singleton &&
			descriptor.ServiceType == typeof(IHostedService) &&
			descriptor.ImplementationType == typeof(FluentOptionsHostedService)
		);
	}
}