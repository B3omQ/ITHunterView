using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using ITHunterview.Service.Resources;

namespace ITHunterview.WebAPI.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env, IStringLocalizer<SharedResource> localizer)
        {
            _next = next;
            _logger = logger;
            _env = env;
            _localizer = localizer;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred during request execution.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = HttpStatusCode.InternalServerError;
            string message = _localizer["SystemError"];

            switch (exception)
            {
                case JobAnalysisException jobAnalysisException:
                    statusCode = (HttpStatusCode)jobAnalysisException.HttpStatus;
                    message = jobAnalysisException.SafeMessage;
                    break;
                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    message = _localizer[exception.Message];
                    break;
                case ArgumentException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = _localizer[exception.Message];
                    break;
                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = _localizer[exception.Message];
                    break;
                case InvalidOperationException:
                    statusCode = HttpStatusCode.Conflict; // 409 — business rule violation
                    message = _localizer[exception.Message];
                    break;
                default:
                    // Trong môi trường phát triển (Development), trả về chi tiết Exception để dễ debug
                    if (_env.IsDevelopment())
                    {
                        message = exception.Message;
                    }
                    break;
            }

            context.Response.StatusCode = (int)statusCode;

            var response = new ResponseBase<object?>
            {
                Success = false,
                Message = message,
                Data = null
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }
}
