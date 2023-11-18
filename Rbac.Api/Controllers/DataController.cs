using System.Net;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Services;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("/api/v1/[controller]")]

public class DataController(ILogger<DataController> logger, IData data) : ControllerBase
{
    private readonly string _eventId = Guid.NewGuid().ToString();

    [HttpPost]
    [Route("get")]
    [ProducesResponseType(typeof(Dictionary<string, object>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<Dictionary<string, object>>> GetAsync([FromBody] Dictionary<string, object> userInfo)
    {
        try
        {
            return new OkObjectResult(await data.GetAsync(userInfo));
        }
        catch (Exception ex)
        {
            logger.LogError(_eventId, ex);
            return BadRequest(ex.Message);
        }
    }


    [HttpPost]
    [Route("update")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> UpdateAsync([FromBody] Dictionary<string, string> device)
    {
        try
        {
            var deviceItem = device.ToDictionary<KeyValuePair<string, string>, string, object>(item => item.Key, item => item.Value);
            await data.UpdateAsync(deviceItem);
            return Accepted();
        }
        catch (Exception ex)
        {
            logger.LogError(_eventId, ex);
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [Route("download")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> DownloadAsync(string fileName)
    {
        try
        {
            return new OkObjectResult(await data.DownloadAsync(fileName));
        }
        catch (Exception ex)
        {
            logger.LogError(_eventId, ex);
            return BadRequest(ex.Message);
        }
    }
    [HttpPost]
    [Route("query")]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> QueryWorkspace()
    {
        try
        {
            return new OkObjectResult(await data.QueryAsync());
        }
        catch (Exception ex)
        {
            logger.LogError(_eventId, ex);
            return BadRequest(ex.Message);
        }
    }
}