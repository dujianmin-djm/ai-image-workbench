using AI.Image.ApiResponse;
using AI.Image.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Http;
using Volo.Abp.Validation;

namespace AI.Image.Filters;

/// <summary>
/// 异常过滤器，处理Controller方法内部的异常
/// </summary>
public class ApiExceptionFilter : IAsyncExceptionFilter
{
	public async Task OnExceptionAsync(ExceptionContext context)
	{
		if (ShouldHandleException(context))
		{
			await HandleApiExceptionAsync(context);
			return;
		}
	}

	private static bool ShouldHandleException(ExceptionContext context)
	{
		if (context.ExceptionHandled)
		{
			return false;
		}
		if (context.ActionDescriptor.IsControllerAction() &&
			context.ActionDescriptor.HasObjectResult())
		{
			return true;
		}
		var request = context.HttpContext.Request;
		var path = request.Path.Value?.ToLowerInvariant() ?? "";
		if (path.StartsWith("/dapi/"))
		{
			return true;
		}
		if (request.CanAccept(MimeTypes.Application.Json))
		{
			return true;
		}
		if (request.IsAjax())
		{
			return true;
		}
		return false;
	}

	private static async Task HandleApiExceptionAsync(ExceptionContext context)
	{
		var exception = context.Exception;
		var services = context.HttpContext.RequestServices;
		var logger = services.GetRequiredService<ILogger<ApiExceptionFilter>>();
		var localizer = services.GetRequiredService<IStringLocalizer<ImageResource>>();
		var exceptionConverter = services.GetRequiredService<IExceptionToErrorInfoConverter>();
		var exceptionOptions = services.GetRequiredService<IOptions<AbpExceptionHandlingOptions>>().Value;
		var errorInfo = exceptionConverter.Convert(exception, options =>
		{
			options.SendExceptionsDetailsToClients = exceptionOptions.SendExceptionsDetailsToClients;
			options.SendStackTraceToClients = exceptionOptions.SendStackTraceToClients;
		});
		LogException(context, exception, errorInfo, logger);
		await context.GetRequiredService<IExceptionNotifier>()
			.NotifyAsync(new ExceptionNotificationContext(exception));
		var (code, message) = ConvertException(exception, errorInfo, localizer);
		var response = ApiResponse<object>.Fail(code, message);
		context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
		context.HttpContext.Response.Headers.Remove(AbpHttpConsts.AbpErrorFormat);
		context.Result = new ObjectResult(response);
		context.ExceptionHandled = true;
	}

	private static (string Code, string Message) ConvertException(
		Exception exception,
		RemoteServiceErrorInfo errorInfo,
		IStringLocalizer<ImageResource> localizer)
	{
		return exception switch
		{
			SysBusinessException pmsEx => (pmsEx.ErrorCode, pmsEx.Message),
			AbpAuthorizationException => (ApiResponseCode.Forbidden, localizer["Auth:Forbidden"].Value),
			BusinessException bizEx => (ApiResponseCode.BadRequest, errorInfo.Message ?? bizEx.Message),
			AbpValidationException validationEx => (ApiResponseCode.ValidationError, FormatValidationErrors(validationEx, errorInfo)),
			EntityNotFoundException => (ApiResponseCode.NotFound, localizer["Auth:NotFound"].Value),
			ArgumentException argEx => (ApiResponseCode.BadRequest, argEx.Message),
			OperationCanceledException ocEx => (ApiResponseCode.BadRequest, ocEx.Message),
			AbpException abpEx => (ApiResponseCode.BadRequest, abpEx.Message),
			_ => (ApiResponseCode.InternalError, localizer["Auth:ServerError"].Value)
		};
	}

	private static string FormatValidationErrors(AbpValidationException exception, RemoteServiceErrorInfo errorInfo)
	{
		if (errorInfo.ValidationErrors != null && errorInfo.ValidationErrors.Length > 0)
		{
			var errors = errorInfo.ValidationErrors.SelectMany(e => e.Members.Select(m => $"{m}: {e.Message}"));
			return string.Join("; ", errors);
		}
		if (exception.ValidationErrors != null && exception.ValidationErrors.Count > 0)
		{
			var errors = exception.ValidationErrors.SelectMany(e => e.MemberNames.Select(m => $"{m}: {e.ErrorMessage}"));
			return string.Join("; ", errors);
		}
		return exception.Message;
	}

	private static void LogException(
		ExceptionContext context, 
		Exception exception,
		RemoteServiceErrorInfo errorInfo,
		ILogger<ApiExceptionFilter> logger)
	{
		var logBuilder = new StringBuilder();
		logBuilder.AppendLine("---------- API Exception ----------");
		logBuilder.AppendLine($"Request Path: {context.HttpContext.Request.Path}");
		if (exception is SysBusinessException pmsEx && !string.IsNullOrEmpty(pmsEx.Details))
		{
			logBuilder.Append($"Details: {pmsEx.Details}");
		}
		if (errorInfo.ValidationErrors != null && errorInfo.ValidationErrors.Length > 0)
		{
			var errors = errorInfo.ValidationErrors.SelectMany(e => e.Members.Select(m => $"{m}: {e.Message}"));
			logBuilder.Append($"Validation Errors: {errors}"); 
		}
		logger.LogWarning(exception, "{ExceptionInfo}", logBuilder.ToString());
	}
}
