using Microsoft.AspNetCore.Mvc;
using VectorSearch.Api.Services;

namespace VectorSearch.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SearchController : ControllerBase
{
    private readonly ILogger<SearchController> _logger;
    private readonly ISearch _search;

    public SearchController(ILogger<SearchController> logger)
    {
        _logger = logger;
        _search = AppSettings.SampleToRun switch
        {
            "TextSearch" => new TextSearch(_logger),
            "BlobSearch" => new BlobSearch(_logger),
            _ => throw new ArgumentException("Search sample is not exists.")
        };
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> Create()
    {
        await _search.CreateIndexAsync();
        return Ok();
    }

    [HttpPost]
    [Route("query")]
    public async Task<IActionResult> Search(string inputQuery, string filter, int searchType)
    {
        _logger.LogTrace("Search");
        var result = await _search.SearchIndexAsync(inputQuery, filter, searchType);
        return Ok(result);
    }
}