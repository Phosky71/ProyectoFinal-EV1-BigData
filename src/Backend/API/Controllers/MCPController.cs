using System.Threading.Tasks;
using Backend.Persistence.Models;
using Backend.MCP.Server;
using Backend.Persistence.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MCPController : ControllerBase
    {
        private readonly MCPServer _server;

        public MCPController(IRepository<Card> repository)
        {
            _server = new MCPServer(repository);
        }

        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] QueryModel model)
        {
            var result = await _server.ProcessQueryAsync(model.Query);
            return Ok(new { response = result });
        }
    }

    public class QueryModel
    {
        public string Query { get; set; }
    }
}
