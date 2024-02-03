using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPlusPlus.Functions;

internal class TestAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var testOptions = context.HttpContext.RequestServices.GetService<TestOptions>();

        if(testOptions.Message!="Nahro")
        {
            context.Result = new UnauthorizedResult();
            return;
        }
    }
}
