using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Persistence.Models;
using Backend.Persistence.Interfaces;
using Backend.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ProyectoFinal.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DataController : ControllerBase
    {
        private readonly IKaggleDataLoader _kaggleLoader;
        private readonly PersistenceManager _persistenceManager;

        public DataController(
            IKaggleDataLoader kaggleLoader,
            PersistenceManager persistenceManager)
        {
            _kaggleLoader = kaggleLoader ?? throw new ArgumentNullException(nameof(kaggleLoader));
            _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));
        }

        private IRepository<Card> CurrentRepository => _persistenceManager.CurrentRepository;

        // ==================== CRUD OPERATIONS ====================

        [HttpGet]
        [ProducesResponseType(typeof(GetAllResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Card>>> GetAll([FromQuery] int? limit = null)
        {
            try
            {
                var items = await CurrentRepository.GetAllAsync();
                var itemsList = items.ToList();

                if (limit.HasValue && limit.Value > 0)
                {
                    itemsList = itemsList.Take(limit.Value).ToList();
                }

                return Ok(new GetAllResponse
                {
                    Count = itemsList.Count,
                    PersistenceMode = await CurrentRepository.GetPersistenceModeAsync(),
                    Data = itemsList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error retrieving data: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Card), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Card>> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "ID is required" });
            }

            try
            {
                var item = await CurrentRepository.GetByIdAsync(id);

                if (item == null)
                {
                    return NotFound(new { error = $"Card with ID '{id}' not found" });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error retrieving card: {ex.Message}" });
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(Card), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Card>> Create([FromBody] Card card)
        {
            if (card == null)
            {
                return BadRequest(new { error = "Card data is required" });
            }

            if (string.IsNullOrWhiteSpace(card.Name))
            {
                return BadRequest(new { error = "Card name is required" });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(card.Id))
                {
                    card.Id = Guid.NewGuid().ToString();
                }

                await CurrentRepository.AddAsync(card);

                return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error creating card: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(string id, [FromBody] Card card)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "ID is required" });
            }

            if (card == null)
            {
                return BadRequest(new { error = "Card data is required" });
            }

            if (id != card.Id)
            {
                return BadRequest(new { error = "ID mismatch between URL and body" });
            }

            try
            {
                var existingItem = await CurrentRepository.GetByIdAsync(id);

                if (existingItem == null)
                {
                    return NotFound(new { error = $"Card with ID '{id}' not found" });
                }

                await CurrentRepository.UpdateAsync(card);

                return Ok(new { message = "Card updated successfully", data = card });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error updating card: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "ID is required" });
            }

            try
            {
                var existingItem = await CurrentRepository.GetByIdAsync(id);

                if (existingItem == null)
                {
                    return NotFound(new { error = $"Card with ID '{id}' not found" });
                }

                await CurrentRepository.DeleteAsync(id);

                return Ok(new { message = "Card deleted successfully", id = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error deleting card: {ex.Message}" });
            }
        }

        // ==================== KAGGLE DATA LOADING ====================

        [HttpPost("load-kaggle")]
        [ProducesResponseType(typeof(LoadKaggleResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> LoadKaggleData()
        {
            try
            {
                var result = await _kaggleLoader.LoadDataAsync();

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Failed to load Kaggle data",
                        details = result.ErrorMessage
                    });
                }

                return Ok(new LoadKaggleResponse
                {
                    Success = true,
                    Message = "Kaggle data loaded successfully",
                    RecordsLoaded = result.RecordsLoaded,
                    PersistenceMode = result.PersistenceMode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error loading Kaggle data: {ex.Message}" });
            }
        }

        [HttpPost("load-to-memory")]
        [ProducesResponseType(typeof(LoadKaggleResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> LoadToMemory()
        {
            try
            {
                var result = await _kaggleLoader.LoadToMemoryAsync();

                return Ok(new LoadKaggleResponse
                {
                    Success = result.Success,
                    Message = result.Success ? "Data loaded to Memory successfully" : "Failed to load data",
                    RecordsLoaded = result.RecordsLoaded,
                    PersistenceMode = "Memory",
                    Error = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error loading to Memory: {ex.Message}" });
            }
        }

        [HttpPost("load-to-mysql")]
        [ProducesResponseType(typeof(LoadKaggleResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> LoadToMySQL()
        {
            try
            {
                var result = await _kaggleLoader.LoadToMySQLAsync();

                return Ok(new LoadKaggleResponse
                {
                    Success = result.Success,
                    Message = result.Success ? "Data loaded to MySQL successfully" : "Failed to load data",
                    RecordsLoaded = result.RecordsLoaded,
                    PersistenceMode = "MySQL",
                    Error = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error loading to MySQL: {ex.Message}" });
            }
        }

        [HttpPost("load-to-both")]
        public async Task<IActionResult> LoadToBoth()
        {
            try
            {
                var (memoryResult, mysqlResult) = await _kaggleLoader.LoadToBothAsync();

                return Ok(new
                {
                    memory = new LoadKaggleResponse
                    {
                        Success = memoryResult.Success,
                        RecordsLoaded = memoryResult.RecordsLoaded,
                        PersistenceMode = "Memory",
                        Error = memoryResult.ErrorMessage
                    },
                    mysql = new LoadKaggleResponse
                    {
                        Success = mysqlResult.Success,
                        RecordsLoaded = mysqlResult.RecordsLoaded,
                        PersistenceMode = "MySQL",
                        Error = mysqlResult.ErrorMessage
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error loading to both systems: {ex.Message}" });
            }
        }

        // ==================== PERSISTENCE MANAGEMENT ====================

        [HttpPost("switch-persistence")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult SwitchPersistence([FromBody] SwitchPersistenceRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Mode))
            {
                return BadRequest(new { error = "Persistence mode is required (Memory or MySQL)" });
            }

            try
            {
                _persistenceManager.SwitchPersistence(request.Mode);

                return Ok(new
                {
                    message = $"Persistence switched to {request.Mode}",
                    currentMode = _persistenceManager.CurrentMode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error switching persistence: {ex.Message}" });
            }
        }

        [HttpGet("persistence-mode")]
        public async Task<IActionResult> GetPersistenceMode()
        {
            try
            {
                var mode = await CurrentRepository.GetPersistenceModeAsync();
                return Ok(new { persistenceMode = mode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error getting persistence mode: {ex.Message}" });
            }
        }

        [HttpDelete("clear")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClearAllData()
        {
            try
            {
                await CurrentRepository.ClearAllAsync();
                return Ok(new { message = "All data cleared successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error clearing data: {ex.Message}" });
            }
        }

        // ==================== STATISTICS & SEARCH ====================

        [HttpGet("stats")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var items = await CurrentRepository.GetAllAsync();
                var itemsList = items.ToList();

                var stats = new
                {
                    totalCards = itemsList.Count,
                    persistenceMode = await CurrentRepository.GetPersistenceModeAsync(),
                    byRarity = itemsList
                        .Where(c => !string.IsNullOrEmpty(c.Rarity))
                        .GroupBy(c => c.Rarity)
                        .Select(g => new { rarity = g.Key, count = g.Count() })
                        .OrderByDescending(x => x.count)
                        .ToList(),
                    byType = itemsList
                        .Where(c => !string.IsNullOrEmpty(c.Type))
                        .GroupBy(c => c.Type?.Split('—')[0].Trim())
                        .Select(g => new { type = g.Key, count = g.Count() })
                        .OrderByDescending(x => x.count)
                        .Take(10)
                        .ToList(),
                    creatures = itemsList.Count(c => c.IsCreature()),
                    withImages = itemsList.Count(c => !string.IsNullOrEmpty(c.ImageUrl))
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error retrieving statistics: {ex.Message}" });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int? limit = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Search query is required" });
            }

            try
            {
                var allItems = await CurrentRepository.GetAllAsync();
                var lowerQuery = query.ToLower();

                var results = allItems
                    .Where(c =>
                        c.Name.ToLower().Contains(lowerQuery) ||
                        (c.Type?.ToLower().Contains(lowerQuery) ?? false) ||
                        (c.Text?.ToLower().Contains(lowerQuery) ?? false))
                    .Take(limit ?? 50)
                    .ToList();

                return Ok(new
                {
                    query = query,
                    count = results.Count,
                    data = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error searching: {ex.Message}" });
            }
        }
    }

    // ==================== RESPONSE MODELS ====================

    public class GetAllResponse
    {
        public int Count { get; set; }
        public string PersistenceMode { get; set; } = string.Empty;
        public IEnumerable<Card> Data { get; set; } = new List<Card>();
    }

    public class LoadKaggleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RecordsLoaded { get; set; }
        public string PersistenceMode { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public class SwitchPersistenceRequest
    {
        [Required]
        public string Mode { get; set; } = string.Empty;
    }
}
