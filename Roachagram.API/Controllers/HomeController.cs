using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Roachagram.API.Controllers
{
    [Route("api/home")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Roachagram API is running.");
        }
    }
}
