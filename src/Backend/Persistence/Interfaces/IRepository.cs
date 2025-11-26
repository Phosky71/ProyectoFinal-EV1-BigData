// IRepository.cs - Interfaz para el patron Open/Close
// Permite cambiar entre persistencia MySQL y Memoria sin modificar el codigo

namespace Backend.Persistence.Interfaces
{
    /// <summary>
    /// Interfaz generica del repositorio (Patron Open/Close)
    /// Implementaciones: MySQLRepository, MemoryRepository
    /// </summary>
    public interface IRepository<T> where T : class
    {
        // Operaciones CRUD
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task<T> CreateAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
        
        // Operaciones de carga masiva (dataset Kaggle)
        Task LoadFromFileAsync(string filePath);
        Task<int> CountAsync();
    }
}
