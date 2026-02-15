using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotnetLambdaLogBase.Logging;

// Assembly attribute to enable the Lambda JSON input serializer
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DotnetLambdaLogBase;

/// <summary>
/// Lambda function template with CloudWatch Logs integration.
/// Demonstrates the FlushAsync pattern for reliable log delivery.
/// </summary>
public class Function
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ILogger<Function> _logger;
    private readonly CloudWatchLoggerProvider _loggerProvider;

    public Function()
    {
        var options = new CloudWatchLoggerOptions
        {
            AllLogsGroupName = Environment.GetEnvironmentVariable("ALL_LOGS_GROUP")
                ?? "/lambda/app/all-logs",
            ErrorLogsGroupName = Environment.GetEnvironmentVariable("ERROR_LOGS_GROUP")
                ?? "/lambda/shared/error-logs",
            FunctionName = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")
        };

        _serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddCloudWatchLogger(o =>
            {
                o.AllLogsGroupName = options.AllLogsGroupName;
                o.ErrorLogsGroupName = options.ErrorLogsGroupName;
                o.FunctionName = options.FunctionName;
            }))
            .BuildServiceProvider();

        _logger = _serviceProvider.GetRequiredService<ILogger<Function>>();
        _loggerProvider = (CloudWatchLoggerProvider)_serviceProvider.GetRequiredService<ILoggerProvider>();
    }

    /// <summary>
    /// Lambda handler entry point.
    /// </summary>
    public async Task<string> FunctionHandler(object input, ILambdaContext context)
    {
        try
        {
            _logger.LogInformation("Processing request: {RequestId}", context.AwsRequestId);

            // E2E test support: throw on specific input to verify error log flush
            var inputStr = input?.ToString() ?? "";
            if (inputStr.Contains("TRIGGER_ERROR"))
            {
                throw new InvalidOperationException("E2E test: intentional error triggered");
            }

            _logger.LogInformation("Request completed: {RequestId}", context.AwsRequestId);
            return "OK";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request: {RequestId}", context.AwsRequestId);
            throw;
        }
        finally
        {
            // Flush logs without disposing ServiceProvider (Lambda reuses containers)
            await _loggerProvider.FlushAsync();
        }
    }
}
