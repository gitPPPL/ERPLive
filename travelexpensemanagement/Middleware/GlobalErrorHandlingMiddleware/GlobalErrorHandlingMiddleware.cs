//using System.Net;
//using System.Text.Json;

//namespace travelexpensemanagement.Middleware.GlobalErrorHandlingMiddleware
//{
//    public class GlobalErrorHandlingMiddleware
//    {
//        private readonly RequestDelegate _next;
//        private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;

//        public GlobalErrorHandlingMiddleware(
//            RequestDelegate next,
//            ILogger<GlobalErrorHandlingMiddleware> logger)
//        {
//            _next = next;
//            _logger = logger;
//        }

//        public async Task Invoke(HttpContext context)
//        {
//            try
//            {
//                await _next(context);

//                // HANDLE HTTP STATUS CODES (NO EXCEPTION)
//                if (context.Response.StatusCode >= 400)
//                {
//                    await HandleStatusCode(context);
//                    return;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Unhandled exception");

//                await HandleException(context, ex);
//            }
//        }

//        private async Task HandleException(HttpContext context, Exception ex)
//        {
//            context.Response.ContentType = "application/json";
//            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

//            var response = new
//            {
//                statusCode = context.Response.StatusCode,
//                message = "Internal server error",
//                detail = ex.Message // remove in production
//            };

//            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
//        }
//        private async Task HandleStatusCode(HttpContext context)
//        {
//            int code = context.Response.StatusCode;

//            if (context.Response.HasStarted)
//                return;

//            // 👉 MVC PAGE REDIRECT
//            context.Response.Redirect(
//                $"/AccessedError/Index?code={code}&message={GetMessage(code)}"
//            );

//            await Task.CompletedTask;
//        }

//        private static string GetMessage(int statusCode)
//        {
//            return statusCode switch
//            {
//                400 => "Bad request",
//                401 => "Your session has expired. Please login again.",
//                403 => "You are not authorized to access this page.",
//                404 => "The page you are looking for does not exist.",
//                429 => "Please try again after 2 minutes",
//                500 => "Internal server error. Please contact administrator.",
//                _ => "Unexpected error occurred."
//            };
//        }
//    }
//}

using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace travelexpensemanagement.Middleware.GlobalErrorHandlingMiddleware
{
    public class GlobalErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;
        
        public GlobalErrorHandlingMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, ErrorLoggerService errorLogger)
        {
            try
            {
                await _next(context);

                // Handle HTTP status codes where no exception occurred
                if (context.Response.StatusCode >= 400)
                {
                    await HandleStatusCode(context, errorLogger);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                await HandleException(context, ex, errorLogger);
            }
        }

        private async Task HandleException(HttpContext context, Exception ex, ErrorLoggerService errorLogger)
        {
            int statusCode = GetStatusCode(ex);
            string errorCategory = GetErrorCategory(ex);
            string errorType = ex.GetType().Name;
            string requestPath = context.Request.Path;
            string errorReference;
            try
            {
                errorReference = errorLogger.LogError(ex, context, statusCode, errorCategory);
            }
            catch (Exception logException)
            {
                // Logging failure should not cause another application error
                _logger.LogError(logException, "Error while saving exception to ErrorLog table");
                errorReference = "N/A";
            }

            if (context.Response.HasStarted)
            {
                return;
            }

            // Check whether request is API/AJAX
            if (IsApiRequest(context))
            {
                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    statusCode = statusCode,
                    errorType = errorCategory,
                    errorCategory = errorCategory,
                    requestPath = requestPath,
                    message = GetMessage(statusCode),
                    errorReference = errorReference
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }

            // Normal MVC request
            context.Response.Clear();
            context.Response.Redirect($"/AccessedError/Index" + $"?code={statusCode}" + $"&message={Uri.EscapeDataString(GetMessage(statusCode))}" + $"&errorReference={Uri.EscapeDataString(errorReference)}" +
                                      $"&errorType={Uri.EscapeDataString(errorType)}" + $"&errorCategory={Uri.EscapeDataString(errorCategory)}" + $"&requestPath={Uri.EscapeDataString(requestPath)}");
        }

        private async Task HandleStatusCode(HttpContext context, ErrorLoggerService errorLogger)
        {
            int code = context.Response.StatusCode;
            if (context.Response.HasStarted)
            {
                return;
            }

            string message = GetMessage(code);

            // Status codes without exceptions
            var exception = new HttpRequestException($"HTTP {code}: {message}");

            string errorCategory = GetStatusCodeCategory(code);
            string errorType = exception.GetType().Name;
            string requestPath = context.Request.Path;
            string errorReference;

            try
            {
                errorReference = errorLogger.LogError(exception, context, code, errorCategory);
            }
            catch (Exception logException)
            {
                _logger.LogError(logException, "Error while saving HTTP status code to ErrorLog table");
                errorReference = "N/A";
            }

            // API/AJAX request
            if (IsApiRequest(context))
            {
                context.Response.Clear();
                context.Response.StatusCode = code;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    statusCode = code,
                    errorType = errorCategory,
                    errorCategory = errorCategory,
                    requestPath = requestPath,
                    message = message,
                    errorReference = errorReference
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));

                return;
            }

            // Normal MVC request
            context.Response.Clear();
            context.Response.Redirect($"/AccessedError/Index" + $"?code={code}" + $"&message={Uri.EscapeDataString(message)}" +  $"&errorReference={Uri.EscapeDataString(errorReference)}" + $"&errorType={Uri.EscapeDataString(errorType)}" +
                                     $"&errorCategory={Uri.EscapeDataString(errorCategory)}" + $"&requestPath={Uri.EscapeDataString(requestPath)}");
        }

        private static int GetStatusCode(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,

                ArgumentException => StatusCodes.Status400BadRequest,

                FormatException => StatusCodes.Status400BadRequest,

                TimeoutException => StatusCodes.Status408RequestTimeout,

                _ => StatusCodes.Status500InternalServerError
            };
        }

        private static string GetErrorCategory(Exception ex)
        {
            if (ex is SqlException)
            {
                return "DatabaseError";
            }

            if (ex is NullReferenceException)
            {
                return "NullReference";
            }

            if (ex is InvalidOperationException)
            {
                // MVC view not found is normally InvalidOperationException
                if (ex.Message.Contains(
                        "The view",
                        StringComparison.OrdinalIgnoreCase) &&
                    ex.Message.Contains(
                        "was not found",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return "ViewNotFound";
                }

                return "InvalidOperation";
            }

            if (ex is FormatException)
            {
                return "FormatError";
            }

            if (ex is ArgumentException)
            {
                return "InvalidArgument";
            }

            if (ex is UnauthorizedAccessException)
            {
                return "AccessDenied";
            }

            if (ex is TimeoutException)
            {
                return "Timeout";
            }

            return "UnexpectedError";
        }

        private static string GetStatusCodeCategory(int statusCode)
        {
            return statusCode switch
            {
                400 => "BadRequest",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "NotFound",
                405 => "MethodNotAllowed",
                408 => "RequestTimeout",
                409 => "Conflict",
                429 => "TooManyRequests",
                500 => "InternalServerError",
                502 => "BadGateway",
                503 => "ServiceUnavailable",
                504 => "GatewayTimeout",
                _ => "HttpError"
            };
        }

        private static string GetMessage(int statusCode)
        {
            return statusCode switch
            {
                400 => "The request could not be processed.",
                401 => "Your session has expired. Please login again.",
                403 => "You are not authorized to access this page.",
                404 => "The page you are looking for does not exist.",
                405 => "This request method is not allowed.",
                408 => "The request took too long to complete.",
                409 => "The request could not be completed because of a conflict.",
                429 => "Too many requests. Please try again later.",
                500 => "Something went wrong while processing your request.",
                502 => "The server received an invalid response.",
                503 => "The service is temporarily unavailable.",
                504 => "The server took too long to respond.",
                _ => "An unexpected error occurred."
            };
        }

        private static bool IsApiRequest(HttpContext context)
        {
            // AJAX request
            if (context.Request.Headers.TryGetValue("X-Requested-With",out var requestedWith))
            {
                if (requestedWith == "XMLHttpRequest")
                {
                    return true;
                }
            }

            // API route
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                return true;
            }

            // JSON request
            if (context.Request.Headers.Accept.Any(
                    x => x.Contains("application/json")))
            {
                return true;
            }

            return false;
        }
    }
}