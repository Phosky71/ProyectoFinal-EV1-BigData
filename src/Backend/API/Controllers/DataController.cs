using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Persistence.Models;
using Backend.Persistence.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoFinal.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere JWT
    public class DataController : ControllerBase
    {
        private readonly IRepository<Card> _repository;
        private readonly IKaggleDataLoader _kaggleLoader; // Servicio para cargar datos de Kaggle

        public DataController(IRepository<Card> repository, IKaggleDataLoader kaggleLoader)
        {
            _repository = repository;
            _kaggleLoader = kaggleLoader;
        }

        /// <summary>
        /// Obtiene todos los datos.
        /// GET: api/data
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Card>>> GetAll()
        {
            try
            {
                var items = await _repository.GetAllAsync();
                return Ok(new
                {
                    count = items.Count(),
                    data = items
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error retrieving data: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene un dato por ID.
        /// GET: api/data/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Card>> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "ID is required" });
            }

            try
            {
                var item = await _repository.GetByIdAsync(id);

                if (item == null)
                {
                    return NotFound(new { error = $"Item with ID {id} not found" });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error retrieving item: {ex.Message}" });
            }
        }

        /// <summary>
        /// Crea un nuevo dato.
        /// POST: api/data
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Card>> Create([FromBody] Card card)
        {
            if (card == null)
            {
                return BadRequest(new { error = "Card data is required" });
            }

            // Validación básica
            if (string.IsNullOrWhiteSpace(card.Name))
            {
                return BadRequest(new { error = "Card name is required" });
            }

            try
            {
                // Generar ID si no existe
                if (string.IsNullOrWhiteSpace(card.Id))
                {
                    card.Id = Guid.NewGuid().ToString();
                }

                await _repository.AddAsync(card);

                return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error creating item: {ex.Message}" });
            }
        }

        /// <summary>
        /// Actualiza un dato existente.
        /// PUT: api/data/{id}
        /// </summary>
        [HttpPut("{id}")]
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
                return BadRequest(new { error = "ID mismatch" });
            }

            try
            {
                var existingItem = await _repository.GetByIdAsync(id);

                if (existingItem == null)
                {
                    return NotFound(new { error = $"Item with ID {id} not found" });
                }

                await _repository.UpdateAsync(card);

                return Ok(new { message = "Item updated successfully", data = card });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error updating item: {ex.Message}" });
            }
        }

        /// <summary>
        /// Elimina un dato.
        /// DELETE: api/data/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "ID is required" });
            }

            try
            {
                var existingItem = await _repository.GetByIdAsync(id);

                if (existingItem == null)
                {
                    return NotFound(new { error = $"Item with ID {id} not found" });
                }

                await _repository.DeleteAsync(id);

                return Ok(new { message = "Item deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error deleting item: {ex.Message}" });
            }
        }

        /// <summary>
        /// NUEVO: Carga los datos del fichero de Kaggle al sistema de persistencia.
        /// POST: api/data/load-kaggle
        /// </summary>
        [HttpPost("load-kaggle")]
        public async Task<IActionResult> LoadKaggleData()
        {
            try
            {
                var result = await _kaggleLoader.LoadDataAsync();

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        error = "Failed to load Kaggle data",
                        details = result.ErrorMessage
                    });
                }

                return Ok(new
                {
                    message = "Kaggle data loaded successfully",
                    recordsLoaded = result.RecordsLoaded,
                    persistenceMode = result.PersistenceMode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error loading Kaggle data: {ex.Message}" });
            }
        }

        /// <summary>
        /// NUEVO: Obtiene estadísticas de los datos cargados.
        /// GET: api/data/stats
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var items = await _repository.GetAllAsync();
                var itemsList = items.ToList();

                var stats = new
                {
                    totalRecords = itemsList.Count,
                    persistenceMode = await _repository.GetPersistenceModeAsync(),
                    // Añade más estadísticas según tu dataset
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error retrieving statistics: {ex.Message}" });
            }
        }
    }
}
