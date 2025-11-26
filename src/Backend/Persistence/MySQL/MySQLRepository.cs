using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Backend.API.Models;
using Backend.Persistence.Interfaces;
using MySql.Data.MySqlClient;

namespace Backend.Persistence.MySQL
{
    public class MySQLRepository : IRepository<Card>
    {
        private readonly string _connectionString;

        public MySQLRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public async Task<IEnumerable<Card>> GetAllAsync()
        {
            var list = new List<Card>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new MySqlCommand("SELECT * FROM Cards", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(MapReaderToCard(reader));
                    }
                }
            }
            return list;
        }

        public async Task<Card> GetByIdAsync(string id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new MySqlCommand("SELECT * FROM Cards WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return MapReaderToCard(reader);
                    }
                }
            }
            return null;
        }

        public async Task AddAsync(Card entity)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new MySqlCommand("INSERT INTO Cards (Id, Name, ManaCost, Type, Rarity, SetName, Text, Power, Toughness, ImageUrl, MultiverseId) VALUES (@Id, @Name, @ManaCost, @Type, @Rarity, @SetName, @Text, @Power, @Toughness, @ImageUrl, @MultiverseId)", conn);
                AddParameters(cmd, entity);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateAsync(Card entity)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new MySqlCommand("UPDATE Cards SET Name=@Name, ManaCost=@ManaCost, Type=@Type, Rarity=@Rarity, SetName=@SetName, Text=@Text, Power=@Power, Toughness=@Toughness, ImageUrl=@ImageUrl, MultiverseId=@MultiverseId WHERE Id=@Id", conn);
                AddParameters(cmd, entity);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteAsync(string id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new MySqlCommand("DELETE FROM Cards WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task LoadDataAsync(string sourcePath)
        {
            // In a real scenario, this might use LOAD DATA INFILE or bulk insert.
            // For this project, we will rely on the SQL script or manual insertion, 
            // but to satisfy the interface, we could implement a bulk insert here if needed.
            // However, the requirement says "Carga de datos a ambos sistemas".
            // So we should implement it. We can reuse the CSV parsing logic from MemoryRepository or use a shared helper.
            
            // For simplicity in this file, we will throw NotImplemented or leave empty if the user is expected to run the SQL script.
            // BUT, the prompt says "Descarga... y carga de datos".
            // So I will implement a basic loop to insert data.
            
            var memoryRepo = new Memory.MemoryRepository();
            await memoryRepo.LoadDataAsync(sourcePath);
            var cards = await memoryRepo.GetAllAsync();
            
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                // Create table if not exists
                var createTableCmd = new MySqlCommand(@"
                    CREATE TABLE IF NOT EXISTS Cards (
                        Id VARCHAR(255) PRIMARY KEY,
                        Name VARCHAR(255),
                        ManaCost VARCHAR(50),
                        Type VARCHAR(255),
                        Rarity VARCHAR(50),
                        SetName VARCHAR(255),
                        Text TEXT,
                        Power VARCHAR(50),
                        Toughness VARCHAR(50),
                        ImageUrl TEXT,
                        MultiverseId VARCHAR(50)
                    )", conn);
                await createTableCmd.ExecuteNonQueryAsync();

                foreach (var card in cards)
                {
                    // Check if exists
                    var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Cards WHERE Id = @Id", conn);
                    checkCmd.Parameters.AddWithValue("@Id", card.Id);
                    long count = (long)await checkCmd.ExecuteScalarAsync();
                    
                    if (count == 0)
                    {
                        var insertCmd = new MySqlCommand("INSERT INTO Cards (Id, Name, ManaCost, Type, Rarity, SetName, Text, Power, Toughness, ImageUrl, MultiverseId) VALUES (@Id, @Name, @ManaCost, @Type, @Rarity, @SetName, @Text, @Power, @Toughness, @ImageUrl, @MultiverseId)", conn);
                        AddParameters(insertCmd, card);
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        private Card MapReaderToCard(IDataReader reader)
        {
            return new Card
            {
                Id = reader["Id"].ToString(),
                Name = reader["Name"].ToString(),
                ManaCost = reader["ManaCost"].ToString(),
                Type = reader["Type"].ToString(),
                Rarity = reader["Rarity"].ToString(),
                SetName = reader["SetName"].ToString(),
                Text = reader["Text"].ToString(),
                Power = reader["Power"].ToString(),
                Toughness = reader["Toughness"].ToString(),
                ImageUrl = reader["ImageUrl"].ToString(),
                MultiverseId = reader["MultiverseId"].ToString()
            };
        }

        private void AddParameters(MySqlCommand cmd, Card entity)
        {
            cmd.Parameters.AddWithValue("@Id", entity.Id);
            cmd.Parameters.AddWithValue("@Name", entity.Name ?? "");
            cmd.Parameters.AddWithValue("@ManaCost", entity.ManaCost ?? "");
            cmd.Parameters.AddWithValue("@Type", entity.Type ?? "");
            cmd.Parameters.AddWithValue("@Rarity", entity.Rarity ?? "");
            cmd.Parameters.AddWithValue("@SetName", entity.SetName ?? "");
            cmd.Parameters.AddWithValue("@Text", entity.Text ?? "");
            cmd.Parameters.AddWithValue("@Power", entity.Power ?? "");
            cmd.Parameters.AddWithValue("@Toughness", entity.Toughness ?? "");
            cmd.Parameters.AddWithValue("@ImageUrl", entity.ImageUrl ?? "");
            cmd.Parameters.AddWithValue("@MultiverseId", entity.MultiverseId ?? "");
        }
    }
}
