using AspireDemo.Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.Json;

namespace AspireDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(ILogger<UserController> logger) : ControllerBase
{
    static List<User> _users;
    static UserController()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "sample_users.json");
        var jsonData = System.IO.File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _users = JsonSerializer.Deserialize<List<User>>(jsonData, options) ?? [];
    }

    private readonly ILogger<UserController> _logger = logger;

    [HttpGet(Name = "GetUsers")]
    [OutputCache]
    public IEnumerable<User> Get()
    {
        Thread.Sleep(3000);
        //create random exception code that can raise exception once in 5 requests to simulate exception
        var random = new Random();
        var randomNumber = random.Next(1, 5);
        if (randomNumber == 3)
        {
            _logger.LogError("Random exception occurred.");
            throw new Exception("Random exception occurred.");
        }

        _logger.LogTrace("Request received.");
        return _users;
    }
}
