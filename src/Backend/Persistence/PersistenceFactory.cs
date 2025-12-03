using System.Configuration;
using Backend.Persistence.Models;
using Backend.Persistence.Interfaces;
using Backend.Persistence.Memory;
using Backend.Persistence.MySQL;

namespace Backend.Persistence
{
    public static class PersistenceFactory
    {
        public static IRepository<Card> GetRepository()
        {
            string persistenceType = ConfigurationManager.AppSettings["PersistenceType"];
            string connectionString = ConfigurationManager.ConnectionStrings["MySQLConnection"]?.ConnectionString;

            if (persistenceType == "MySQL" && !string.IsNullOrEmpty(connectionString))
            {
                return new MySQLRepository(connectionString);
            }
            else
            {
                return new MemoryRepository();
            }
        }
    }
}
