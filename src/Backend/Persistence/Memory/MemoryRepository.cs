using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            return await Task.FromResult(_data.Values.ToList());
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

            _data.TryAdd(entity.Id, entity);
            await Task.CompletedTask;
        }

        public async Task UpdateAsync(Card entity)
        {
            if (_data.ContainsKey(entity.Id))
            {
                _data[entity.Id] = entity;
            }
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string id)
        {
            _data.TryRemove(id, out _);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Carga datos desde un CSV de Kaggle usando CsvHelper (robusto).
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
                    BadDataFound = null // Ignorar datos malformados
                });

                // Mapeo manual de columnas del CSV de MTG de Kaggle
                var records = csv.GetRecords<dynamic>();

                await Task.Run(() =>
                {
                    foreach (var record in records)
                    {
                        try
                        {
                            var dict = record as IDictionary<string, object>;

                            var card = new Card
                            {
                                Id = GetValue(dict, "id") ?? Guid.NewGuid().ToString(),
                                Name = GetValue(dict, "name") ?? "Unknown",
                                ManaCost = GetValue(dict, "manaCost"),
                                Type = GetValue(dict, "type"),
                                Rarity = GetValue(dict, "rarity"),
                                SetName = GetValue(dict, "setName") ?? GetValue(dict, "set"),
                                Text = GetValue(dict, "text"),
                                Power = GetValue(dict, "power"),
                                Toughness = GetValue(dict, "toughness"),
                                ImageUrl = GetValue(dict, "imageUrl"),
                                MultiverseId = GetValue(dict, "multiverseid") ?? GetValue(dict, "multiverseId")
                            };

                            _data.TryAdd(card.Id, card);
                            loadedCount++;
                        }
                        catch
                        {
                            // Continuar con la siguiente línea si hay error
                        }
                    }
                });

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
            await Task.CompletedTask;
        }

        // Helper para extraer valores del diccionario dinámico de CsvHelper
        private string? GetValue(IDictionary<string, object>? dict, string key)
        {
            if (dict == null) return null;

            if (dict.TryGetValue(key, out var value))
            {
                return value?.ToString()?.Trim();
            }

            return null;
        }
    }
}
