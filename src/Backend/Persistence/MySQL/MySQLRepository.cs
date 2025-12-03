using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
                        Name VARCHAR(255) NOT NULL,
                        ManaCost VARCHAR(50),
                        Type VARCHAR(255),
                        Rarity VARCHAR(50),
                        SetName VARCHAR(255),
                        Text TEXT,
                        Power VARCHAR(50),
                        Toughness VARCHAR(50),
                        ImageUrl TEXT,
                        MultiverseId VARCHAR(50),
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        UpdatedAt TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                ", conn);

                await createTableCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize MySQL database: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<Card>> GetAllAsync()
        {
            var list = new List<Card>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("SELECT * FROM Cards", conn);
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

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(@"
                INSERT INTO Cards (Id, Name, ManaCost, Type, Rarity, SetName, Text, Power, Toughness, ImageUrl, MultiverseId) 
                VALUES (@Id, @Name, @ManaCost, @Type, @Rarity, @SetName, @Text, @Power, @Toughness, @ImageUrl, @MultiverseId)
            ", conn);

            AddParameters(cmd, entity);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Card entity)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(@"
                UPDATE Cards 
                SET Name=@Name, ManaCost=@ManaCost, Type=@Type, Rarity=@Rarity, SetName=@SetName, 
                    Text=@Text, Power=@Power, Toughness=@Toughness, ImageUrl=@ImageUrl, MultiverseId=@MultiverseId 
                WHERE Id=@Id
            ", conn);

            AddParameters(cmd, entity);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(string id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("DELETE FROM Cards WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Carga datos desde CSV de Kaggle usando MemoryRepository como intermediario.
        /// Implementa bulk insert para mejor rendimiento.
        /// </summary>
        public async Task<int> LoadDataAsync(string sourcePath)
        {
            // Usar MemoryRepository para parsear el CSV (reutilizar código)
            var tempMemoryRepo = new Memory.MemoryRepository();
            var loadedCount = await tempMemoryRepo.LoadDataAsync(sourcePath);
            var cards = (await tempMemoryRepo.GetAllAsync()).ToList();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // Usar transacción para bulk insert eficiente
            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                foreach (var card in cards)
                {
                    // Check if exists (evitar duplicados)
                    var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Cards WHERE Id = @Id", conn, transaction as MySqlTransaction);
                    checkCmd.Parameters.AddWithValue("@Id", card.Id);
                    var count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

                    if (count == 0)
                    {
                        var insertCmd = new MySqlCommand(@"
                            INSERT INTO Cards (Id, Name, ManaCost, Type, Rarity, SetName, Text, Power, Toughness, ImageUrl, MultiverseId) 
                            VALUES (@Id, @Name, @ManaCost, @Type, @Rarity, @SetName, @Text, @Power, @Toughness, @ImageUrl, @MultiverseId)
                        ", conn, transaction as MySqlTransaction);

                        AddParameters(insertCmd, card);
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
                return loadedCount;
            }
            catch
            {
                await transaction.RollbackAsync();
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
                MultiverseId = reader["MultiverseId"]?.ToString()
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
