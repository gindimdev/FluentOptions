using GDD.FluentOptions.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GDD.FluentOptions;

public static class FluentOptionsExtensions
{
	public static OptionsBuilder<TOptions> AddFluentOptions<TOptions, THelper>(this IServiceCollection services)
		where TOptions : class
		where THelper : FluentOptionsHelper<TOptions>
	{
		services.AddSingleton<IPostConfigureOptions<TOptions>, THelper>();
		services.Configure<FluentOptionsHostedService.Options>(
			static o => o.AddOptions<TOptions, THelper>()
		);
		return services.AddOptions<TOptions>();
	}

	public static IServiceCollection AddFluentOptionsStartupCheck(this IServiceCollection services)
		=> services.AddHostedService<FluentOptionsHostedService>();
}