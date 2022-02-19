using Microsoft.AspNetCore.Mvc;

namespace ServiceSpy.Example.ApiService;

/// <summary>
/// Test api controller
/// </summary>
[ApiController]
[Route("/test")]
public class TestApiController : ControllerBase
{
    /// <summary>
    /// Test api
    /// </summary>
    /// <returns>Text</returns>
    [Produces("text/plain")]
    [HttpGet("test")]
    public string Test()
    {
        return "It Works";
    }
}
