using System.Text.Json.Serialization;

namespace AspireDemo.Common.Entities;


public class User(string userId, string firstName, string lastName, string email, string department, string tenantId, DateOnly dateJoined)
{
    [JsonPropertyName("userId")]
    public required string UserId { get; init; } = userId;

    [JsonPropertyName("firstName")]
    public required string FirstName { get; init; } = firstName;

    [JsonPropertyName("lastName")]
    public required string LastName { get; init; } = lastName;

    [JsonPropertyName("email")]
    public required string Email { get; init; } = email;

    [JsonPropertyName("department")]
    public required string Department { get; init; } = department;

    [JsonPropertyName("tenantId")]
    public required string TenantId { get; init; } = tenantId;

    [JsonPropertyName("dateJoined")]
    public DateOnly DateJoined { get; set; } = dateJoined;
}


[JsonSerializable(typeof(List<User>))]
public sealed partial class UserSerializerContext : JsonSerializerContext
{
}