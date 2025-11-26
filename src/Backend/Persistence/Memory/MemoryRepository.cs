using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProyectoFinal.Backend.Persistence.Interfaces;

namespace ProyectoFinal.Backend.Persistence.Memory
{
    /// <summary>
    /// Implementacion del repositorio usando memoria (List + LINQ) como sistema de persistencia.
    /// Implementa el patron Open/Close permitiendo intercambiar con MySQLRepository.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad</typeparam>
    public class MemoryRepository<T> : IRepository<T> where T : class, IEntity, new()
    {
        private readonly List<T> _data;
        private int _nextId;
        private readonly object _lock = new();

        public MemoryRepository()
        {
            _data = new List<T>();
            _nextId = 1;
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            lock (_lock)
            {
                // Usar LINQ para consultas
                var result = _data.AsEnumerable();
                return Task.FromResult(result);
            }
        }

        public Task<T?> GetByIdAsync(int id)
        {
            lock (_lock)
            {
                // LINQ: Buscar por ID
                var entity = _data.FirstOrDefault(e => e.Id == id);
                return Task.FromResult(entity);
            }
        }

        public Task<T> AddAsync(T entity)
        {
            lock (_lock)
            {
                entity.Id = _nextId++;
                _data.Add(entity);
                return Task.FromResult(entity);
            }
        }

        public Task<T> UpdateAsync(T entity)
        {
            lock (_lock)
            {
                // LINQ: Encontrar indice del elemento
                var index = _data.FindIndex(e => e.Id == entity.Id);
                
                if (index >= 0)
                {
                    _data[index] = entity;
                }
                
                return Task.FromResult(entity);
            }
        }

        public Task<bool> DeleteAsync(int id)
        {
            lock (_lock)
            {
                // LINQ: Remover elemento que coincida
                var removed = _data.RemoveAll(e => e.Id == id);
                return Task.FromResult(removed > 0);
            }
        }

        public async Task<int> LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Archivo no encontrado: {filePath}");
            }

            var extension = Path.GetExtension(filePath).ToLower();
            var count = 0;

            lock (_lock)
            {
                _data.Clear();
                _nextId = 1;
            }

            if (extension == ".json")
            {
                count = await LoadFromJsonAsync(filePath);
            }
            else if (extension == ".csv")
            {
                count = await LoadFromCsvAsync(filePath);
            }
            else
            {
                throw new NotSupportedException($"Formato no soportado: {extension}");
            }

            return count;
        }

        private async Task<int> LoadFromJsonAsync(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var items = JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            
            lock (_lock)
            {
                foreach (var item in items)
                {
                    item.Id = _nextId++;
                    _data.Add(item);
                }
            }
            
            return items.Count;
        }

        private async Task<int> LoadFromCsvAsync(string filePath)
        {
            // TODO: Implementar parseo CSV usando reflexion
            var lines = await File.ReadAllLinesAsync(filePath);
            // Procesar lineas...
            return lines.Length - 1; // Excluir header
        }

        // Metodos LINQ adicionales para consultas avanzadas
        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                return _data.Where(predicate).ToList();
            }
        }

        public T? FirstOrDefault(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                return _data.FirstOrDefault(predicate);
            }
        }

        public int Count()
        {
            lock (_lock)
            {
                return _data.Count;
            }
        }
    }

    /// <summary>
    /// Interface base para entidades con ID.
    /// </summary>
    public interface IEntity
    {
        int Id { get; set; }
    }
}
