using AspireDemo.Common.Entities;
using System.Text.Json;

namespace AspireDemo.Web.Services;

public class UserService(HttpClient httpClient)
{
    private HttpClient httpClient = httpClient;

    public async Task<List<User>> GetUsers()
    {
        List<User>? users = null;
        var response = await httpClient.GetAsync("/api/user");
        if (response.IsSuccessStatusCode)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            users = await response.Content.ReadFromJsonAsync(UserSerializerContext.Default.ListUser);
        }

        return users ?? [];
    }
}
