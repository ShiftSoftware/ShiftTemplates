using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ShiftEntity.Functions.AppCheck;
using ShiftSoftware.ShiftEntity.Functions.ReCaptcha;
using System.ComponentModel.DataAnnotations;

namespace StockPlusPlus.Functions.Functions;

public class Function
{
    private readonly ILogger<Function> _logger;

    public Function(ILogger<Function> logger)
    {
        _logger = logger;
    }

    [Function("ReCaptcha")]
    [CheckReCaptcha(6)]
    public IActionResult ReCaptcha([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [Function("ModelValidation")]
    public IActionResult ModelValidation([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [Microsoft.Azure.Functions.Worker.Http.FromBody] LoginDTO dto)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult(dto);
    }

    [Function("AppCheck")]
    [CheckAppCheck]
    public IActionResult AppCheck([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        /*
        POST / v1beta / projects / 905344726828 / apps / 1:905344726828:android: d5cf8f1b298a7695: exchangeDebugToken? key = {your-key} HTTP / 1.1
        Host: firebaseappcheck.googleapis.com
        Content - Type: application / json
        Content - Length: 62

        {
            "debugToken": "9DBDE480-B818-4F60-B13A-F4B360B5E98D"
        }
        */

        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}

public class LoginDTO
{
    [Required]
    [MaxLength(5)]
    public string Username { get; set; }

    public string Password { get; set; }
}

public class LoginValidator : AbstractValidator<LoginDTO>
{
    public LoginValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MinimumLength(3).Equal(x=> x.Username);
    }
}
