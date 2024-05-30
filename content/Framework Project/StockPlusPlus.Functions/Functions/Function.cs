using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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
}

public class LoginDTO
{
    [Required]
    [MaxLength(10)]
    public string Username { get; set; }

    [Required]
    [MinLength(3)]
    public string Password { get; set; }
}
