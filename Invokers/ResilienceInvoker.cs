using Polly;
using Polly.Wrap;
using Grpc.Core;

namespace WolfBankGateway.Invokers;

public class ResilienceInvoker
{
  private readonly AsyncPolicyWrap _policyWrap;
  private readonly ILogger<ResilienceInvoker> _logger;

  public ResilienceInvoker(ILogger<ResilienceInvoker> logger)
  {
    _logger = logger;

    var handledExceptions = Policy
        .Handle<Exception>(ex =>
            ex switch
            {
              RpcException grpcEx => grpcEx.StatusCode == StatusCode.Unavailable
                               || grpcEx.StatusCode == StatusCode.DeadlineExceeded
                               || grpcEx.StatusCode == StatusCode.Internal,
              HttpRequestException => true,
              TaskCanceledException => true,
              _ => false
            });

    var retryPolicy = handledExceptions
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    var circuitBreakerPolicy = handledExceptions
        .AdvancedCircuitBreakerAsync(
            failureThreshold: 0.7,
            samplingDuration: TimeSpan.FromSeconds(30),
            minimumThroughput: 10,
            durationOfBreak: TimeSpan.FromSeconds(15),
            onBreak: (ex, breakDelay, context) =>
            {
              _logger.LogInformation($"[Circuit Breaker] Broken due to: {ex.Message}");
            },
            onReset: context =>
            {
              _logger.LogInformation("[Circuit Breaker] Reset");
            },
            onHalfOpen: () =>
            {
              _logger.LogInformation("[Circuit Breaker] Testing connection...");
            });

    _policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
  }

  public async Task<TResponse> ExecuteAsync<TResponse>(Func<Task<TResponse>> grpcCall)
  {
    return await _policyWrap.ExecuteAsync(async () =>
    {
      return await grpcCall();
    });
  }

  public async Task ExecuteAsync(Func<Task> call)
  {
    await _policyWrap.ExecuteAsync(async () => await call());
  }
}
