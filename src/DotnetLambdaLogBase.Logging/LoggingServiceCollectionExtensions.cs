using Amazon.CloudWatchLogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetLambdaLogBase.Logging;

/// <summary>
/// Extension methods for registering CloudWatch Logger with DI.
/// </summary>
public static class LoggingServiceCollectionExtensions
{
    public static ILoggingBuilder AddCloudWatchLogger(this ILoggingBuilder builder, Action<CloudWatchLoggerOptions> configure)
    {
        var options = new CloudWatchLoggerOptions();
        configure(options);

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IAmazonCloudWatchLogs>(new AmazonCloudWatchLogsClient());
        builder.Services.AddSingleton<ILogFormatter, JsonLogFormatter>();
        builder.Services.AddSingleton<ILogSender>(sp =>
            new CloudWatchLogSender(sp.GetRequiredService<IAmazonCloudWatchLogs>(), sp.GetRequiredService<ILogFormatter>()));
        builder.Services.AddSingleton<ILoggerProvider>(sp =>
            new CloudWatchLoggerProvider(options, sp.GetRequiredService<ILogSender>()));

        return builder;
    }
}
