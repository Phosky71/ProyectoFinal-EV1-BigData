using System.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        [HttpPost("persistence")]
        public IActionResult SetPersistence([FromBody] PersistenceModel model)
        {
            // In a real app, this would be thread-safe or per-request. 
            // For this assignment, we update the in-memory config or the file.
            // We'll update the ConfigurationManager (which might not persist to file but affects runtime).
            
            System.Configuration.ConfigurationManager.AppSettings["PersistenceType"] = model.Type;
            return Ok(new { message = $"Persistence switched to {model.Type}" });
        }
    }

    public class PersistenceModel
    {
        public string Type { get; set; } // "Memory" or "MySQL"
    }
}
