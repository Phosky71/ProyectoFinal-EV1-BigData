using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace ProyectoFinal.Backend.API.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private string _currentMode;

        public ConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            _currentMode = _configuration["Persistence:Mode"] ?? "Memory";
        }

        public async Task<string> GetPersistenceModeAsync()
        {
            return await Task.FromResult(_currentMode);
        }

        public async Task SetPersistenceModeAsync(string mode)
        {
            if (mode != "Memory" && mode != "MySQL")
            {
                throw new ArgumentException($"Invalid persistence mode: {mode}");
            }
            _currentMode = mode;
            await Task.CompletedTask;
        }
    }
}
