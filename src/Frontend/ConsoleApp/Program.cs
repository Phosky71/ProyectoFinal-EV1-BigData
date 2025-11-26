using System;
using System.Threading.Tasks;

namespace ProyectoFinal.Frontend.ConsoleApp
{
    /// <summary>
    /// Aplicacion de consola para el proyecto.
    /// Funcionalidades:
    /// - Login de usuario
    /// - Seleccion de sistema de persistencia (MySQL/Memoria)
    /// - Carga de datos desde Kaggle
    /// - Operaciones CRUD
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=".PadRight(50, '='));
            Console.WriteLine("  PROYECTO FINAL EV1 - BIG DATA & AI");
            Console.WriteLine("  Aplicacion de Consola");
            Console.WriteLine("=".PadRight(50, '='));
            Console.WriteLine();

            // TODO: Implementar flujo principal
            await RunApplicationAsync();
        }

        static async Task RunApplicationAsync()
        {
            bool isAuthenticated = false;
            string currentUser = "";

            // 1. Login
            while (!isAuthenticated)
            {
                isAuthenticated = await LoginAsync();
                if (!isAuthenticated)
                {
                    Console.WriteLine("Credenciales incorrectas. Intente nuevamente.\n");
                }
                else
                {
                    currentUser = "Usuario"; // TODO: Obtener del token
                }
            }

            // 2. Seleccionar sistema de persistencia
            var persistence = SelectPersistenceSystem();

            // 3. Menu principal
            await ShowMainMenuAsync(persistence);
        }

        static async Task<bool> LoginAsync()
        {
            Console.WriteLine("--- LOGIN ---");
            Console.Write("Usuario: ");
            var username = Console.ReadLine();
            Console.Write("Contrasena: ");
            var password = ReadPassword();
            Console.WriteLine();

            // TODO: Validar credenciales con API JWT
            await Task.Delay(500); // Simular llamada a API
            return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
        }

        static string SelectPersistenceSystem()
        {
            Console.WriteLine("\n--- SELECCIONAR SISTEMA DE PERSISTENCIA ---");
            Console.WriteLine("1. MySQL (Base de datos)");
            Console.WriteLine("2. Memoria (LINQ)");
            Console.Write("Seleccione opcion: ");

            var option = Console.ReadLine();
            return option == "1" ? "MySQL" : "Memory";
        }

        static async Task ShowMainMenuAsync(string persistence)
        {
            Console.WriteLine($"\n[Sistema: {persistence}]");
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("\n--- MENU PRINCIPAL ---");
                Console.WriteLine("1. Cargar datos desde archivo (Kaggle)");
                Console.WriteLine("2. Listar registros");
                Console.WriteLine("3. Agregar registro");
                Console.WriteLine("4. Actualizar registro");
                Console.WriteLine("5. Eliminar registro");
                Console.WriteLine("6. Salir");
                Console.Write("Seleccione opcion: ");

                var option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await LoadDataFromFileAsync();
                        break;
                    case "2":
                        await ListRecordsAsync();
                        break;
                    case "3":
                        await AddRecordAsync();
                        break;
                    case "4":
                        await UpdateRecordAsync();
                        break;
                    case "5":
                        await DeleteRecordAsync();
                        break;
                    case "6":
                        exit = true;
                        Console.WriteLine("\nHasta luego!");
                        break;
                    default:
                        Console.WriteLine("Opcion no valida.");
                        break;
                }
            }
        }

        // TODO: Implementar metodos CRUD
        static async Task LoadDataFromFileAsync()
        {
            Console.Write("Ruta del archivo: ");
            var path = Console.ReadLine();
            Console.WriteLine("Cargando datos...");
            await Task.Delay(1000);
            Console.WriteLine("Datos cargados exitosamente.");
        }

        static async Task ListRecordsAsync()
        {
            Console.WriteLine("Obteniendo registros...");
            await Task.Delay(500);
            Console.WriteLine("[Lista de registros]");
        }

        static async Task AddRecordAsync()
        {
            Console.WriteLine("[Agregar nuevo registro]");
            await Task.Delay(100);
        }

        static async Task UpdateRecordAsync()
        {
            Console.Write("ID del registro a actualizar: ");
            Console.ReadLine();
            await Task.Delay(100);
        }

        static async Task DeleteRecordAsync()
        {
            Console.Write("ID del registro a eliminar: ");
            Console.ReadLine();
            await Task.Delay(100);
        }

        static string ReadPassword()
        {
            var password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            return password;
        }
    }
}
