using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BLL.Services;
using BO.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters
{
    public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
    {

        private readonly IDictionary<Type, Action<ExceptionContext>> _exceptionHandlers;
        private readonly ITracesService _traceService;
        public ApiExceptionFilterAttribute(ITracesService traceService)
        {

            _traceService = traceService;

            // Register known exception types and handlers.
            _exceptionHandlers = new Dictionary<Type, Action<ExceptionContext>>
            {
                { typeof(NotFoundException), HandleNotFoundException },
                { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
                { typeof(ForbiddenAccessException), HandleForbiddenAccessException },
		{ typeof(DoublonException), HandleDoublonException },
            };
        }

        public override void OnException(ExceptionContext context)
        {
            HandleException(context);

            base.OnException(context);
        }

        /// <summary>
        /// Handle the good exception for the context
        /// </summary>
        /// <param name="context">Context of exception</param>
        private void HandleException(ExceptionContext context)
        {
            Type type = context.Exception.GetType();
            if (_exceptionHandlers.ContainsKey(type))
            {
                var iNotLoggedException  = type as INotLoggedException;

                if (iNotLoggedException == null)
                {
                    _traceService.Trace(context.Exception.Message);
                }
                     
                _exceptionHandlers[type].Invoke(context);
                return;
            }

            if (!context.ModelState.IsValid)
            {
                HandleInvalidModelStateException(context);
                return;
            }

            HandleUnknownException(context);
        }

        /// <summary>
        /// Handle a validation model exception 
        /// </summary>
        /// <param name="context">Context Exception</param>
        //private void HandleValidationException(ExceptionContext context)
        //{
        //    var exception = context.Exception as ValidationException;

        //    var details = new ValidationProblemDetails(exception?.Errors)
        //    {
        //        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        //    };

        //    context.Result = new BadRequestObjectResult(details);

        //    context.ExceptionHandled = true;
        //}

        /// <summary>
        /// Handle Invalid Model State exception 
        /// </summary>
        /// <param name="context">Context of exception</param>
        private void HandleInvalidModelStateException(ExceptionContext context)
        {
            var details = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            };

            context.Result = new BadRequestObjectResult(details);

            context.ExceptionHandled = true;
        }

        /// <summary>
        /// Handle a not found ressource exception 
        /// </summary>
        /// <param name="context">Context of exception</param>
        private void HandleNotFoundException(ExceptionContext context)
        {
            var exception = context.Exception as NotFoundException;

            var details = new ProblemDetails()
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "The specified resource was not found.",
                Detail = exception?.Message
            };

            context.Result = new NotFoundObjectResult(details);

            context.ExceptionHandled = true;
        }

        /// <summary>
        /// Handle for Unauthorized Access Exception
        /// </summary>
        /// <param name="context">Context of the exception</param>
        private void HandleUnauthorizedAccessException(ExceptionContext context)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            };

            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };

            context.ExceptionHandled = true;
        }

        /// <summary>
        /// Handle a forbidden Access excception
        /// </summary>
        /// <param name="context">Context of the exception</param>
        private void HandleForbiddenAccessException(ExceptionContext context)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
            };

            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

            context.ExceptionHandled = true;
        }

        /// <summary>
        /// Handle for an Unknow Exception
        /// </summary>
        /// <param name="context">Context of the exception</param>
        private void HandleUnknownException(ExceptionContext context)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };

            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            context.ExceptionHandled = true;
        }

	/// <summary>
        /// Handle for a duplicate ressource Exception
        /// </summary>
        /// <param name="context">Context of the exception</param>
        public void HandleDoublonException(ExceptionContext context)
        {
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict.",
                Type = "https://tools.ietf.org/html/rfc7231#page-60", 
                Detail = "La ressource existe déjà."
            };

            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status409Conflict
            };

            _logger.LogError(context.Exception.ToString());

            context.ExceptionHandled = true;
        }
    }
}