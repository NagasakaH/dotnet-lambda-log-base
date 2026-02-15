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

    public Function()
    {
        _serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddCloudWatchLogger(options =>
                {
                    options.AllLogsGroupName = Environment.GetEnvironmentVariable("ALL_LOGS_GROUP")
                        ?? "/lambda/app/all-logs";
                    options.ErrorLogsGroupName = Environment.GetEnvironmentVariable("ERROR_LOGS_GROUP")
                        ?? "/lambda/shared/error-logs";
                    options.FunctionName = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME");
                });
            })
            .BuildServiceProvider();

        _logger = _serviceProvider.GetRequiredService<ILogger<Function>>();
    }

    /// <summary>
    /// Lambda handler entry point.
    /// </summary>
    public async Task<string> FunctionHandler(object input, ILambdaContext context)
    {
        try
        {
            _logger.LogInformation("Processing request: {RequestId}", context.AwsRequestId);

            // TODO: Implement business logic here

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
            // Ensure logs are flushed before Lambda freezes
            await _serviceProvider.DisposeAsync();
        }
    }
}
