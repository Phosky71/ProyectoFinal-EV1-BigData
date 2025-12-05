using System.Data;
using Backend.Persistence.Models;
using Backend.Persistence.Interfaces;
using MySql.Data.MySqlClient;

namespace Backend.Persistence.MySQL
{
    /// <summary>
    /// Repositorio con persistencia en MySQL.
    /// Cumple patrón Open/Close junto con MemoryRepository.
    /// </summary>
    public class MySQLRepository : IRepository<Card>
    {
        private readonly string _connectionString;

        public MySQLRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Crea la tabla Cards si no existe.
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                var createTableCmd = new MySqlCommand(@"
                    CREATE TABLE IF NOT EXISTS Cards (
                        Id VARCHAR(255) PRIMARY KEY,
                        Name VARCHAR(200) NOT NULL,
                        ManaCost VARCHAR(50),
                        Type VARCHAR(200),
                        Rarity VARCHAR(50),
                        SetName VARCHAR(200),
                        Text VARCHAR(1000),
                        Power VARCHAR(10),
                        Toughness VARCHAR(10),
                        ImageUrl VARCHAR(500),
                        MultiverseId VARCHAR(50),
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        UpdatedAt TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
                        INDEX idx_name (Name),
                        INDEX idx_type (Type),
                        INDEX idx_rarity (Rarity)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                ", conn);

                await createTableCmd.ExecuteNonQueryAsync();
                Console.WriteLine("MySQL table 'Cards' initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize MySQL database: {ex.Message}");
                throw new Exception($"Failed to initialize MySQL database: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<Card>> GetAllAsync()
        {
            var list = new List<Card>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("SELECT * FROM Cards ORDER BY Name", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToCard(reader));
            }

            return list;
        }

        public async Task<Card?> GetByIdAsync(string id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("SELECT * FROM Cards WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapReaderToCard(reader);
            }

            return null;
        }

        public async Task AddAsync(Card entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                entity.Id = Guid.NewGuid().ToString();
            }

            entity.CreatedAt = DateTime.UtcNow;

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(@"
                INSERT INTO Cards (Id, Name, ManaCost, Type, Rarity, SetName, Text, Power, Toughness, ImageUrl, MultiverseId, CreatedAt) 
                VALUES (@Id, @Name, @ManaCost, @Type, @Rarity, @SetName, @Text, @Power, @Toughness, @ImageUrl, @MultiverseId, @CreatedAt)
            ", conn);

            AddParameters(cmd, entity);
            cmd.Parameters.AddWithValue("@CreatedAt", entity.CreatedAt);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Card entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(@"
                UPDATE Cards 
                SET Name=@Name, ManaCost=@ManaCost, Type=@Type, Rarity=@Rarity, SetName=@SetName, 
                    Text=@Text, Power=@Power, Toughness=@Toughness, ImageUrl=@ImageUrl, MultiverseId=@MultiverseId,
                    UpdatedAt=@UpdatedAt
                WHERE Id=@Id
            ", conn);

            AddParameters(cmd, entity);
            cmd.Parameters.AddWithValue("@UpdatedAt", entity.UpdatedAt);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                throw new KeyNotFoundException($"Card with ID '{entity.Id}' not found");
            }
        }

        public async Task DeleteAsync(string id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("DELETE FROM Cards WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                throw new KeyNotFoundException($"Card with ID '{id}' not found");
            }
        }

        /// <summary>
        /// Carga datos desde CSV de Kaggle usando MemoryRepository como intermediario.
        /// Implementa bulk insert para mejor rendimiento.
        /// </summary>
        public async Task<int> LoadDataAsync(string sourcePath)
        {
            // Usar MemoryRepository para parsear el CSV (reutilizar código)
            var tempMemoryRepo = new Memory.MemoryRepository();
            await tempMemoryRepo.LoadDataAsync(sourcePath);
            var cards = (await tempMemoryRepo.GetAllAsync()).ToList();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // Limpiar tabla antes de cargar
            var clearCmd = new MySqlCommand("TRUNCATE TABLE Cards", conn);
            await clearCmd.ExecuteNonQueryAsync();

            // Usar transacción para bulk insert eficiente
            using var transaction = await conn.BeginTransactionAsync();
            int insertedCount = 0;

            try
            {
                foreach (var card in cards)
                {
                    var insertCmd = new MySqlCommand(@"
                        INSERT INTO Cards (Id, Name, ManaCost, Type, Rarity, SetName, Text, Power, Toughness, ImageUrl, MultiverseId, CreatedAt) 
                        VALUES (@Id, @Name, @ManaCost, @Type, @Rarity, @SetName, @Text, @Power, @Toughness, @ImageUrl, @MultiverseId, @CreatedAt)
                        ON DUPLICATE KEY UPDATE 
                            Name=VALUES(Name), ManaCost=VALUES(ManaCost), Type=VALUES(Type),
                            Rarity=VALUES(Rarity), SetName=VALUES(SetName), Text=VALUES(Text),
                            Power=VALUES(Power), Toughness=VALUES(Toughness), ImageUrl=VALUES(ImageUrl),
                            MultiverseId=VALUES(MultiverseId), UpdatedAt=CURRENT_TIMESTAMP
                    ", conn, transaction as MySqlTransaction);

                    AddParameters(insertCmd, card);
                    insertCmd.Parameters.AddWithValue("@CreatedAt", card.CreatedAt);

                    await insertCmd.ExecuteNonQueryAsync();
                    insertedCount++;
                }

                await transaction.CommitAsync();
                Console.WriteLine($"Loaded {insertedCount} cards into MySQL");
                return insertedCount;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error loading data into MySQL: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetPersistenceModeAsync()
        {
            return await Task.FromResult("MySQL");
        }

        public async Task ClearAllAsync()
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("TRUNCATE TABLE Cards", conn);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("MySQL table cleared");
        }

        // ==================== MÉTODOS PRIVADOS ====================

        private Card MapReaderToCard(IDataReader reader)
        {
            return new Card
            {
                Id = reader["Id"]?.ToString() ?? string.Empty,
                Name = reader["Name"]?.ToString() ?? string.Empty,
                ManaCost = reader["ManaCost"]?.ToString(),
                Type = reader["Type"]?.ToString(),
                Rarity = reader["Rarity"]?.ToString(),
                SetName = reader["SetName"]?.ToString(),
                Text = reader["Text"]?.ToString(),
                Power = reader["Power"]?.ToString(),
                Toughness = reader["Toughness"]?.ToString(),
                ImageUrl = reader["ImageUrl"]?.ToString(),
                MultiverseId = reader["MultiverseId"]?.ToString(),
                CreatedAt = reader["CreatedAt"] is DBNull ? DateTime.UtcNow : Convert.ToDateTime(reader["CreatedAt"]),
                UpdatedAt = reader["UpdatedAt"] is DBNull ? null : Convert.ToDateTime(reader["UpdatedAt"])
            };
        }

        private void AddParameters(MySqlCommand cmd, Card entity)
        {
            cmd.Parameters.AddWithValue("@Id", entity.Id);
            cmd.Parameters.AddWithValue("@Name", entity.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@ManaCost", entity.ManaCost ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Type", entity.Type ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Rarity", entity.Rarity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SetName", entity.SetName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Text", entity.Text ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Power", entity.Power ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Toughness", entity.Toughness ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ImageUrl", entity.ImageUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@MultiverseId", entity.MultiverseId ?? (object)DBNull.Value);
        }
    }
}
