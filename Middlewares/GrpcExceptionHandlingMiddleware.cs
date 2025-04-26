using Grpc.Core;
using System.Net;
using System.Text.Json;

namespace WolfBankGateway.Middlewares;

public class GrpcExceptionHandlingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<GrpcExceptionHandlingMiddleware> _logger;

  private static int _totalRequests = 0;
  private static int _failedRequests = 0;
  private static DateTime _windowStart = DateTime.UtcNow;
  private static readonly TimeSpan WindowSize = TimeSpan.FromMinutes(1);
  private static readonly object _lock = new();

  public GrpcExceptionHandlingMiddleware(RequestDelegate next, ILogger<GrpcExceptionHandlingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task Invoke(HttpContext context)
  {
    var isError = false;

    try
    {
      await _next(context);
    }
    catch (RpcException grpcException)
    {
      isError = true;

      _logger.LogError(grpcException, "gRPC call failed with status {Status}", grpcException.Status.StatusCode);

      context.Response.ContentType = "application/json";
      context.Response.StatusCode = MapGrpcStatusToHttpStatus(grpcException.Status.StatusCode);

      var errorResponse = new
      {
        error = grpcException.Status.Detail,
        grpcStatus = grpcException.Status.StatusCode.ToString(),
      };

      await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
    catch (Exception ex)
    {
      isError = true;

      _logger.LogError(ex, "An unexpected error occurred");

      context.Response.ContentType = "application/json";
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

      var errorResponse = new
      {
        error = "An unexpected error occurred",
      };

      await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
    finally
    {
      CountAndLogErrorRate(isError);
    }
  }

  private void CountAndLogErrorRate(bool isError)
  {
    lock (_lock)
    {
      _totalRequests++;
      if (isError) _failedRequests++;

      if (DateTime.UtcNow - _windowStart >= WindowSize)
      {
        var errorRate = _totalRequests == 0
            ? 0
            : (_failedRequests * 100.0 / _totalRequests);

        _logger.LogWarning("Error rate (last 1m): {Rate:F2}% ({Failed}/{Total})",
            errorRate, _failedRequests, _totalRequests);

        _totalRequests = 0;
        _failedRequests = 0;
        _windowStart = DateTime.UtcNow;
      }
    }
  }

  private static int MapGrpcStatusToHttpStatus(StatusCode grpcStatusCode)
  {

    return grpcStatusCode switch
    {
      StatusCode.OK => (int)HttpStatusCode.OK,
      StatusCode.InvalidArgument => (int)HttpStatusCode.BadRequest,
      StatusCode.NotFound => (int)HttpStatusCode.NotFound,
      StatusCode.PermissionDenied => (int)HttpStatusCode.Forbidden,
      StatusCode.Unauthenticated => (int)HttpStatusCode.Unauthorized,
      StatusCode.AlreadyExists => (int)HttpStatusCode.Conflict,
      StatusCode.FailedPrecondition => (int)HttpStatusCode.BadRequest,
      StatusCode.ResourceExhausted => (int)HttpStatusCode.TooManyRequests,
      StatusCode.Unavailable => (int)HttpStatusCode.ServiceUnavailable,
      StatusCode.DeadlineExceeded => (int)HttpStatusCode.GatewayTimeout,
      StatusCode.Unimplemented => (int)HttpStatusCode.NotImplemented,
      StatusCode.Internal or StatusCode.Unknown => (int)HttpStatusCode.InternalServerError,
      _ => (int)HttpStatusCode.InternalServerError
    };
  }
}
