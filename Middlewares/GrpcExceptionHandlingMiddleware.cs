using Grpc.Core;
using System.Net;
using System.Text.Json;

namespace WolfBankGateway.Middlewares;

public class GrpcExceptionHandlingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<GrpcExceptionHandlingMiddleware> _logger;

  public GrpcExceptionHandlingMiddleware(RequestDelegate next, ILogger<GrpcExceptionHandlingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task Invoke(HttpContext context)
  {
    try
    {
      await _next(context);
    }
    catch (RpcException grpcException)
    {
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
      _logger.LogError(ex, "An unexpected error occurred");

      context.Response.ContentType = "application/json";
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

      var errorResponse = new
      {
        error = "An unexpected error occurred",
      };

      await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
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
