using Grpc.Core;
using Grpc.Core.Interceptors;

public class GrpcTraceInterceptor : Interceptor
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public GrpcTraceInterceptor(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  private Metadata AddTraceId(Metadata headers)
  {
    headers = headers ?? new Metadata();

    var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
    headers.Add("x-trace-id", traceId);

    return headers;
  }

  public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
      TRequest request,
      ClientInterceptorContext<TRequest, TResponse> context,
      AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
  {
    var newOptions = context.Options.WithHeaders(AddTraceId(context.Options.Headers));
    var newContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, newOptions);

    return base.AsyncUnaryCall(request, newContext, continuation);
  }
}
