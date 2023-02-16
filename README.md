# Fluent options

```csharp
public class SampleOptions
{
	public required string EncodedInput { get; set; }
	public string Message { get; private set; } = null!;

	public class Helper : FluentOptionsHelper<SampleOptions>
	{
		public Helper()
		{
			Validate(static validator =>
			{
				validator.RuleFor(static o => o.EncodedInput).NotEmpty();
			});
			Configure(static options =>
			{
				options.Message = Encoding.Default.GetString(Convert.FromBase64String(options.EncodedInput));
			});
			Validate(static validator =>
			{
				validator.RuleFor(static o => o.Message).NotEqual("Hello world");
			});
		}
	}
}
```
```csharp
services.AddFluentOptions<SampleOptions, SampleOptions.Helper>();

services.AddFluentOptionsStartupCheck(); // Optional
```
In this example `EncodedInput` may come from configuration (e.g. appsettings.json),\
and `Message` may be used by dependent services the `SampleOptions` are injected into.

The `EncodedInput` is validated not to be an empty string, converted from base64 and the result is validated to not equal "Hello World".
If at any step there is a failure, an appropriate [exception](./src/FluentOptions/FluentOptionsExceptions.cs) is thrown.

The `FluentOptionsHelper<>` implements `IPostConfigureOptions<>` and is run whenever `IOptions<>.Value` is accessed.\
The exceptions may be handled by middleware or, if the startup check is added, on `app.Run()`.