using System.Threading.Tasks;

namespace Backend.Persistence.Interfaces
{
    /// <summary>
    /// Servicio que carga los datos del dataset de Kaggle al sistema de persistencia.
    /// </summary>
    public interface IKaggleDataLoader
    {
        /// <summary>
        /// Carga datos al sistema de persistencia activo.
        /// </summary>
        Task<KaggleLoadResult> LoadDataAsync();

        /// <summary>
        /// Carga datos específicamente a Memory.
        /// </summary>
        Task<KaggleLoadResult> LoadToMemoryAsync();

        /// <summary>
        /// Carga datos específicamente a MySQL.
        /// </summary>
        Task<KaggleLoadResult> LoadToMySQLAsync();

        /// <summary>
        /// Carga datos a ambos sistemas de persistencia.
        /// </summary>
        Task<(KaggleLoadResult Memory, KaggleLoadResult MySQL)> LoadToBothAsync();
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
