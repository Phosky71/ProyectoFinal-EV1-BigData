using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Persistence.Interfaces
{
    /// <summary>
    /// Interfaz genérica para persistencia (patrón Open/Close).
    /// Permite implementaciones en Memoria (LINQ) y MySQL.
    /// </summary>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Obtiene todas las entidades.
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Obtiene una entidad por ID.
        /// </summary>
        Task<T?> GetByIdAsync(string id);

        /// <summary>
        /// Añade una nueva entidad.
        /// </summary>
        Task AddAsync(T entity);

        /// <summary>
        /// Actualiza una entidad existente.
        /// </summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Elimina una entidad por ID.
        /// </summary>
        Task DeleteAsync(string id);

        /// <summary>
        /// Carga datos desde un fichero de Kaggle (CSV/JSON).
        /// </summary>
        Task<int> LoadDataAsync(string sourcePath);

        /// <summary>
        /// Obtiene el modo de persistencia actual ("Memory" o "MySQL").
        /// </summary>
        Task<string> GetPersistenceModeAsync();

        /// <summary>
        /// Limpia todos los datos (útil para testing).
        /// </summary>
        Task ClearAllAsync();
    }
}
