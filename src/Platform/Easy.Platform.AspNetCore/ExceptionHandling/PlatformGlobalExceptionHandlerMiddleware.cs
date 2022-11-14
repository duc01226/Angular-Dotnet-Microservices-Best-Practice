using System.Net;
using Easy.Platform.Application.Context.UserContext;
using Easy.Platform.Application.Exceptions;
using Easy.Platform.AspNetCore.Middleware.Abstracts;
using Easy.Platform.Common.Exceptions;
using Easy.Platform.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Easy.Platform.AspNetCore.ExceptionHandling;

/// <summary>
/// This middleware should be used it at the first level to catch general exception from any next middleware.
/// </summary>
public class PlatformGlobalExceptionHandlerMiddleware : PlatformMiddleware
{
    private const string DefaultServerErrorMessage =
        "There is an unexpected error during the processing of the request. Please try again or contact the Administrator for help.";

    private readonly bool developerExceptionEnabled;
    private readonly IPlatformApplicationUserContextAccessor userContextAccessor;

    public PlatformGlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<PlatformGlobalExceptionHandlerMiddleware> logger,
        IConfiguration configuration,
        IPlatformApplicationUserContextAccessor userContextAccessor) : base(next)
    {
        Logger = logger;
        this.userContextAccessor = userContextAccessor;
        developerExceptionEnabled = configuration.GetValue<bool>("DeveloperExceptionEnabled");
    }

    protected ILogger Logger { get; }

    protected override async Task InternalInvokeAsync(HttpContext context)
    {
        try
        {
            await Next(context);
        }
        catch (Exception e)
        {
            await OnException(context, e);
        }
    }

    protected virtual Task OnException(HttpContext context, Exception exception)
    {
        if (exception is BadHttpRequestException or OperationCanceledException)
        {
            Logger.LogWarning(exception, exception.GetType().Name);
            return Task.CompletedTask;
        }

        var errorResponse = exception
            .WhenIs<Exception, PlatformPermissionException, PlatformAspNetMvcErrorResponse>(
                permissionException => new PlatformAspNetMvcErrorResponse(
                    PlatformAspNetMvcErrorInfo.FromPermissionException(permissionException),
                    HttpStatusCode.Forbidden,
                    context.TraceIdentifier).Pipe(_ => LogKnownRequestWarning(exception, context)))
            .WhenIs<IPlatformValidationException>(
                validationException => new PlatformAspNetMvcErrorResponse(
                    PlatformAspNetMvcErrorInfo.FromValidationException(validationException),
                    HttpStatusCode.BadRequest,
                    context.TraceIdentifier).Pipe(_ => LogKnownRequestWarning(exception, context)))
            .WhenIs<PlatformApplicationException>(
                applicationException => new PlatformAspNetMvcErrorResponse(
                    PlatformAspNetMvcErrorInfo.FromApplicationException(applicationException),
                    HttpStatusCode.BadRequest,
                    context.TraceIdentifier).Pipe(_ => LogKnownRequestWarning(exception, context)))
            .WhenIs<PlatformNotFoundException>(
                domainNotFoundException => new PlatformAspNetMvcErrorResponse(
                    PlatformAspNetMvcErrorInfo.FromNotFoundException(domainNotFoundException),
                    HttpStatusCode.NotFound,
                    context.TraceIdentifier).Pipe(_ => LogKnownRequestWarning(exception, context)))
            .WhenIs<PlatformDomainException>(
                domainException => new PlatformAspNetMvcErrorResponse(
                    PlatformAspNetMvcErrorInfo.FromDomainException(domainException),
                    HttpStatusCode.BadRequest,
                    context.TraceIdentifier).Pipe(_ => LogKnownRequestWarning(exception, context)))
            .Else(
                exception =>
                {
                    Logger.LogError(
                        exception,
                        "[UnexpectedRequestError] There is an unexpected exception during the processing of the request. RequestId: {requestId}. UserContext: {UserContext}",
                        context.TraceIdentifier,
                        userContextAccessor.Current.GetAllKeyValues().AsJson());

                    return new PlatformAspNetMvcErrorResponse(
                        new PlatformAspNetMvcErrorInfo
                        {
                            Code = "InternalServerException",
                            Message = developerExceptionEnabled ? exception.ToString() : DefaultServerErrorMessage
                        },
                        HttpStatusCode.InternalServerError,
                        context.TraceIdentifier);
                })
            .Execute();

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = errorResponse.StatusCode;
        return context.Response.WriteAsync(errorResponse.AsJson(), context.RequestAborted);
    }

    protected void LogKnownRequestWarning(Exception exception, HttpContext context)
    {
        Logger.LogWarning(
            exception,
            "[KnownRequestWarning] There is a {exceptionType} during the processing of the request. RequestId: {requestId}",
            exception.GetType(),
            context.TraceIdentifier);
    }
}