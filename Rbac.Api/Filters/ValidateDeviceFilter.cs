using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rbac.Api.Extensions;

namespace Rbac.Api.Filters;

public class ValidateDeviceFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        return;
    }
}