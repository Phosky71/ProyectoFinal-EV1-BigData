using System.Threading.Tasks;

namespace ProyectoFinal.Backend.API.Services
{
    public interface IConfigurationService
    {
        Task<string> GetPersistenceModeAsync();
        Task SetPersistenceModeAsync(string mode);
    }
}
