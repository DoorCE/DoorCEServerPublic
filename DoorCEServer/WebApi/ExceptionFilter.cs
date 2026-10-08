using System.Security.Authentication;
using DoorCEServer.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace DoorCEServer.WebApi;

public class HttpResponseExceptionFilter : IActionFilter, IOrderedFilter
{
    public int Order => int.MaxValue - 10;

    public void OnActionExecuting(ActionExecutingContext context) { }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        switch (context.Exception) {
            case null:
                return;
            case ArgumentException:
            case FileValidationException:
                context.Result = new BadRequestObjectResult(context.Exception.Message); break; // 400
            case AuthenticationException:
                context.Result = new ObjectResult(context.Exception.Message){StatusCode=401}; break;
            case InvalidOperationException:
            case UnauthorizedAccessException:
                context.Result = new ObjectResult(context.Exception.Message){StatusCode=403}; break;
            case NotFoundOrVisibleException: 
                context.Result = new NotFoundObjectResult(context.Exception.Message); break; // 404
            default:
                context.Result = new ObjectResult(context.Exception.Message){StatusCode=500}; break;
        }
        context.ExceptionHandled = true;
        Log.Error($"Exception raised: {context.Exception.Message},\n"+
                      $"Stack trace:\n{context.Exception.StackTrace}%%%%%%");
    }
}