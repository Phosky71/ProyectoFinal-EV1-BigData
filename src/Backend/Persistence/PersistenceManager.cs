using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;
using Backend.Persistence.Memory;
using Backend.Persistence.MySQL;

namespace Backend.Persistence;

/// <summary>
/// Gestor de persistencia que permite cambiar dinámicamente entre Memory y MySQL.
/// Implementa el patrón Open/Close permitiendo extender con nuevos sistemas sin modificar código existente.
/// </summary>
public class PersistenceManager
{
    private readonly MemoryRepository _memoryRepository;
    private readonly MySQLRepository _mySQLRepository;
    private IRepository<Card> _currentRepository;
    private string _currentMode;

    public IRepository<Card> CurrentRepository => _currentRepository;
    public string CurrentMode => _currentMode;

    public PersistenceManager(MemoryRepository memoryRepository, MySQLRepository mySQLRepository, string initialMode)
    {
        _memoryRepository = memoryRepository;
        _mySQLRepository = mySQLRepository;
        _currentMode = initialMode;

        _currentRepository = initialMode.Equals("MySQL", StringComparison.OrdinalIgnoreCase)
            ? _mySQLRepository
            : _memoryRepository;
    }

    /// <summary>
    /// Cambia el sistema de persistencia activo.
    /// </summary>
    public void SwitchPersistence(string mode)
    {
        if (mode.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
        {
            _currentRepository = _mySQLRepository;
            _currentMode = "MySQL";
        }
        else
        {
            _currentRepository = _memoryRepository;
            _currentMode = "Memory";
        }

        Console.WriteLine($"🔄 Persistencia cambiada a: {_currentMode}");
    }

    /// <summary>
    /// Obtiene el repositorio Memory (para cargas de datos).
    /// </summary>
    public MemoryRepository GetMemoryRepository() => _memoryRepository;

    /// <summary>
    /// Obtiene el repositorio MySQL (para cargas de datos).
    /// </summary>
    public MySQLRepository GetMySQLRepository() => _mySQLRepository;
}
