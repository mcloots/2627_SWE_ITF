using ITFPulse.Contracts.Health;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ITFPulse.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HealthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public ActionResult<HealthResponse> Get()
        {
            var serviceName = _configuration["Service:Name"] ?? "ITF Pulse API";

            var response = new HealthResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = serviceName
            };

            return Ok(response);
        }
    }
}
