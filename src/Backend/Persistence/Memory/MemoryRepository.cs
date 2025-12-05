using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Concurrent;
using System.Globalization;

namespace Backend.Persistence.Memory
{
    /// <summary>
    /// Repositorio en memoria usando LINQ (según requisito del enunciado).
    /// Thread-safe con ConcurrentDictionary.
    /// </summary>
    public class MemoryRepository : IRepository<Card>
    {
        // ConcurrentDictionary es thread-safe (mejor que List<Card> estático)
        private static readonly ConcurrentDictionary<string, Card> _data = new();

        public async Task<IEnumerable<Card>> GetAllAsync()
        {
            // LINQ sobre datos en memoria (requisito del enunciado)
            return await Task.FromResult(_data.Values.OrderBy(c => c.Name).ToList());
        }

        public async Task<Card?> GetByIdAsync(string id)
        {
            await Task.CompletedTask;
            _data.TryGetValue(id, out var card);
            return card;
        }

        public async Task AddAsync(Card entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                entity.Id = Guid.NewGuid().ToString();
            }

            entity.CreatedAt = DateTime.UtcNow;
            _data.TryAdd(entity.Id, entity);
            await Task.CompletedTask;
        }

        public async Task UpdateAsync(Card entity)
        {
            if (_data.ContainsKey(entity.Id))
            {
                entity.UpdatedAt = DateTime.UtcNow;
                _data[entity.Id] = entity;
            }
            else
            {
                throw new KeyNotFoundException($"Card with ID '{entity.Id}' not found");
            }
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string id)
        {
            if (!_data.TryRemove(id, out _))
            {
                throw new KeyNotFoundException($"Card with ID '{id}' not found");
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Carga datos desde un CSV de Kaggle usando CsvHelper - Mapeo directo a Card.
        /// </summary>
        public async Task<int> LoadDataAsync(string sourcePath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Dataset file not found: {sourcePath}");
            }

            _data.Clear();
            int loadedCount = 0;

            try
            {
                using var reader = new StreamReader(sourcePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null, // Ignorar campos faltantes
                    BadDataFound = null, // Ignorar datos malformados
                    TrimOptions = TrimOptions.Trim
                });

                // Mapeo directo a Card usando los atributos [Name]
                var cards = csv.GetRecords<Card>();

                await Task.Run(() =>
                {
                    foreach (var card in cards)
                    {
                        try
                        {
                            // Generar ID si no existe o está vacío
                            if (string.IsNullOrWhiteSpace(card.Id))
                            {
                                card.Id = Guid.NewGuid().ToString();
                            }

                            // Asegurar que Name no sea nulo
                            if (string.IsNullOrWhiteSpace(card.Name))
                            {
                                card.Name = "Unknown";
                            }

                            card.CreatedAt = DateTime.UtcNow;
                            _data.TryAdd(card.Id, card);
                            loadedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($" Error loading card: {ex.Message}");
                        }
                    }
                });

                Console.WriteLine($" Loaded {loadedCount} cards into Memory");
                return loadedCount;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading data from Kaggle CSV: {ex.Message}", ex);
            }
        }

        public async Task<string> GetPersistenceModeAsync()
        {
            return await Task.FromResult("Memory");
        }

        public async Task ClearAllAsync()
        {
            _data.Clear();
            Console.WriteLine(" Memory cleared");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Método adicional: búsqueda con LINQ (ejemplo de uso de LINQ en memoria).
        /// </summary>
        public async Task<IEnumerable<Card>> SearchAsync(string query)
        {
            var lowerQuery = query.ToLower();

            // Ejemplo de LINQ sobre datos en memoria
            var results = _data.Values
                .Where(c =>
                    c.Name.ToLower().Contains(lowerQuery) ||
                    (c.Type?.ToLower().Contains(lowerQuery) ?? false) ||
                    (c.Text?.ToLower().Contains(lowerQuery) ?? false))
                .OrderBy(c => c.Name)
                .ToList();

            return await Task.FromResult(results);
        }
    }
}
