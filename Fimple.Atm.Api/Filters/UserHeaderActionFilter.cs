using Fimple.Atm.Domain.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fimple.Atm.Api.Filters
{
    public class UserHeaderActionFilter : IActionFilter
    {
        public const string USER_HEADER_NAME = "X-User";

        public void OnActionExecuting(ActionExecutingContext actionExecutingContext)
        {
            var userName = actionExecutingContext.HttpContext.Request.Headers[USER_HEADER_NAME].FirstOrDefault();
            var normalizedUserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();

            foreach (var argument in actionExecutingContext.ActionArguments.Values)
            {
                if (argument is UserRequestBase userRequest)
                {
                    userRequest.SetUserName(normalizedUserName);
                    break;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
