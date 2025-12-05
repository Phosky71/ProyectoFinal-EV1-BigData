using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;

namespace Backend.Persistence
{
    /// <summary>
    /// Servicio que carga los datos del dataset de Kaggle al sistema de persistencia.
    /// Soporta carga en Memory y MySQL según el PersistenceManager.
    /// </summary>
    public class KaggleDataLoader : IKaggleDataLoader
    {
        private readonly PersistenceManager _persistenceManager;
        private readonly string _datasetPath;

        public KaggleDataLoader(PersistenceManager persistenceManager, string datasetPath)
        {
            _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));
            _datasetPath = datasetPath ?? throw new ArgumentNullException(nameof(datasetPath));
        }

        /// <summary>
        /// Carga los datos del CSV de Kaggle al sistema de persistencia activo.
        /// </summary>
        public async Task<KaggleLoadResult> LoadDataAsync()
        {
            try
            {
                if (!File.Exists(_datasetPath))
                {
                    return new KaggleLoadResult
                    {
                        Success = false,
                        RecordsLoaded = 0,
                        PersistenceMode = _persistenceManager.CurrentMode,
                        ErrorMessage = $"Dataset file not found: {_datasetPath}"
                    };
                }

                Console.WriteLine($"Loading Kaggle dataset from: {_datasetPath}");
                Console.WriteLine($"Target persistence: {_persistenceManager.CurrentMode}");

                var recordsLoaded = await _persistenceManager.CurrentRepository.LoadDataAsync(_datasetPath);

                Console.WriteLine($"Successfully loaded {recordsLoaded} cards into {_persistenceManager.CurrentMode}");

                return new KaggleLoadResult
                {
                    Success = true,
                    RecordsLoaded = recordsLoaded,
                    PersistenceMode = _persistenceManager.CurrentMode,
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Kaggle data: {ex.Message}");

                return new KaggleLoadResult
                {
                    Success = false,
                    RecordsLoaded = 0,
                    PersistenceMode = _persistenceManager.CurrentMode,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Carga los datos específicamente en Memory (LINQ).
        /// </summary>
        public async Task<KaggleLoadResult> LoadToMemoryAsync()
        {
            try
            {
                if (!File.Exists(_datasetPath))
                {
                    return new KaggleLoadResult
                    {
                        Success = false,
                        RecordsLoaded = 0,
                        PersistenceMode = "Memory",
                        ErrorMessage = $"Dataset file not found: {_datasetPath}"
                    };
                }

                var memoryRepo = _persistenceManager.GetMemoryRepository();
                var recordsLoaded = await memoryRepo.LoadDataAsync(_datasetPath);

                Console.WriteLine($"Loaded {recordsLoaded} cards into Memory");

                return new KaggleLoadResult
                {
                    Success = true,
                    RecordsLoaded = recordsLoaded,
                    PersistenceMode = "Memory",
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading to Memory: {ex.Message}");

                return new KaggleLoadResult
                {
                    Success = false,
                    RecordsLoaded = 0,
                    PersistenceMode = "Memory",
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Carga los datos específicamente en MySQL.
        /// </summary>
        public async Task<KaggleLoadResult> LoadToMySQLAsync()
        {
            try
            {
                if (!File.Exists(_datasetPath))
                {
                    return new KaggleLoadResult
                    {
                        Success = false,
                        RecordsLoaded = 0,
                        PersistenceMode = "MySQL",
                        ErrorMessage = $"Dataset file not found: {_datasetPath}"
                    };
                }

                var mysqlRepo = _persistenceManager.GetMySQLRepository();
                var recordsLoaded = await mysqlRepo.LoadDataAsync(_datasetPath);

                Console.WriteLine($"Loaded {recordsLoaded} cards into MySQL");

                return new KaggleLoadResult
                {
                    Success = true,
                    RecordsLoaded = recordsLoaded,
                    PersistenceMode = "MySQL",
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading to MySQL: {ex.Message}");

                return new KaggleLoadResult
                {
                    Success = false,
                    RecordsLoaded = 0,
                    PersistenceMode = "MySQL",
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Carga los datos en AMBOS sistemas de persistencia.
        /// </summary>
        public async Task<(KaggleLoadResult Memory, KaggleLoadResult MySQL)> LoadToBothAsync()
        {
            var memoryResult = await LoadToMemoryAsync();
            var mysqlResult = await LoadToMySQLAsync();

            return (memoryResult, mysqlResult);
        }
    }
}
