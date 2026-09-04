using Microsoft.AspNetCore.Mvc.Filters;
using CleanArchitectureTemplate_Application.Exceptions;

namespace CleanArchitectureTemplate_Api.Filters
{
    public class ValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(msg => !string.IsNullOrEmpty(msg));

                throw new BadRequestException(string.Join(" | ", errors));
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
