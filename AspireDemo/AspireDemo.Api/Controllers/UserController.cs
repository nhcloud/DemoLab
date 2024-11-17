using AspireDemo.Common.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AspireDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(ILogger<UserController> logger) : ControllerBase
{
    static List<User>? _users = new();
    static UserController()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "sample_users.json");
        var jsonData = System.IO.File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _users = JsonSerializer.Deserialize<List<User>>(jsonData, options);
    }

    private readonly ILogger<UserController> _logger = logger;

    [HttpGet(Name = "GetUsers")]
    public IEnumerable<User> Get()
    {
        _logger.LogTrace("Request received.");
        return _users;
    }
}
