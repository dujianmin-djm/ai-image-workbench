namespace AI.Image.Middlewares;

public static class ApplicationBuilderExtensions
{
	/// <summary>
	/// Api响应处理中间件
	/// </summary>
	public static IApplicationBuilder UseApiResponseHandler(this IApplicationBuilder builder)
	{
		return builder.UseMiddleware<ApiResponseHandlerMiddleware>();
	}
}
