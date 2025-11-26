using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Persistence.Interfaces
{
    public interface IRepository<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(string id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(string id);
        Task LoadDataAsync(string sourcePath); // For loading from CSV/JSON
    }
}
