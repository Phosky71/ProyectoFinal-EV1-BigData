using System;
using System.Threading.Tasks;
using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;

namespace Backend.Persistence
{
    public class KaggleDataLoader : IKaggleDataLoader
    {
        private readonly IRepository<Card> _repository;
        private readonly string _datasetPath;

        public KaggleDataLoader(IRepository<Card> repository, string datasetPath)
        {
            _repository = repository;
            _datasetPath = datasetPath;
        }

        public async Task<KaggleLoadResult> LoadDataAsync()
        {
            try
            {
                var recordsLoaded = await _repository.LoadDataAsync(_datasetPath);
                var persistenceMode = await _repository.GetPersistenceModeAsync();

                return new KaggleLoadResult
                {
                    Success = true,
                    RecordsLoaded = recordsLoaded,
                    PersistenceMode = persistenceMode
                };
            }
            catch (Exception ex)
            {
                return new KaggleLoadResult
                {
                    Success = false,
                    RecordsLoaded = 0,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
