using Microsoft.Extensions.Logging;
using System;
using Volo.Abp;

namespace AI.Image.ApiResponse;

public class SysBusinessException : BusinessException
{
	public string ErrorCode { get; }
	public SysBusinessException(
		string code,
		string message,
		string? details = null, 
		Exception? innerException = null,
		LogLevel logLevel = LogLevel.Error)
		: base(message: message, details: details, innerException: innerException, logLevel: logLevel)
	{
		ErrorCode = code;
	}
}
