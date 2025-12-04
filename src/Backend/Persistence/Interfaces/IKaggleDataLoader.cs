using System.Threading.Tasks;

namespace Backend.Persistence.Interfaces
{
    /// <summary>
    /// Servicio que carga los datos del dataset de Kaggle al sistema de persistencia.
    /// </summary>
    public interface IKaggleDataLoader
    {
        Task<KaggleLoadResult> LoadDataAsync();
    }

    /// <summary>
    /// Resultado de la operación de carga de datos.
    /// </summary>
    public class KaggleLoadResult
    {
        public bool Success { get; set; }
        public int RecordsLoaded { get; set; }
        public string PersistenceMode { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
