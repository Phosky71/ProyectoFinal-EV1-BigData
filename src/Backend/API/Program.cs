using Backend.MCP;
using Backend.MCP.Interfaces;
using Backend.MCP.Routers;
using Backend.Persistence;
using Backend.Persistence.Interfaces;
using Backend.Persistence.Memory;
using Backend.Persistence.Models;
using Backend.Persistence.MySQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProyectoFinal.Backend.API.Auth;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURACIÓN ====================

var configuration = builder.Configuration;

// ==================== SERVICIOS ====================

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Proyecto Final API",
        Version = "v1",
        Description = "API REST con JWT, MCP y persistencia Memory/MySQL (Patrón Open/Close)"
    });

    // Añadir JWT a Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==================== JWT AUTHENTICATION ====================

var jwtSecret = configuration["Jwt:Secret"] ?? "THIS_IS_A_VERY_LONG_SECRET_KEY_FOR_JWT_AUTHENTICATION_MINIMUM_32_CHARACTERS";
var jwtIssuer = configuration["Jwt:Issuer"] ?? "ProyectoFinalEV1";
var jwtAudience = configuration["Jwt:Audience"] ?? "ProyectoFinalEV1Users";
var jwtExpirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");

builder.Services.AddSingleton(sp =>
    new JwtService(jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes)
);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ==================== PERSISTENCIA (Patrón Open/Close) ====================

var persistenceMode = configuration["Persistence:Mode"] ?? "Memory";
var connectionString = configuration.GetConnectionString("MySQL") ?? "Server=localhost;Database=proyecto_final;User=root;Password=root;";

// Registrar AMBAS implementaciones para permitir el cambio dinámico
builder.Services.AddSingleton<MemoryRepository>();
builder.Services.AddSingleton(sp => new MySQLRepository(connectionString));

// Registrar PersistenceManager para gestionar el cambio dinámico
builder.Services.AddSingleton<PersistenceManager>(sp =>
{
    var memoryRepo = sp.GetRequiredService<MemoryRepository>();
    var mysqlRepo = sp.GetRequiredService<MySQLRepository>();
    return new PersistenceManager(memoryRepo, mysqlRepo, persistenceMode);
});

// Registrar IRepository<Card> usando el PersistenceManager
builder.Services.AddSingleton<IRepository<Card>>(sp =>
    sp.GetRequiredService<PersistenceManager>().CurrentRepository
);

Console.WriteLine($"Persistence mode inicial: {persistenceMode}");

// ==================== MCP (Model Context Protocol) ====================

// HttpClientFactory para LLMRouter
builder.Services.AddHttpClient();

// Routers
builder.Services.AddSingleton<IRuleRouter>(sp =>
    new RuleRouter(sp.GetRequiredService<IRepository<Card>>())
);

builder.Services.AddSingleton<ILLMRouter>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key no configurada");
    return new LLMRouter(httpClientFactory, apiKey);
});

// MCP Service
builder.Services.AddSingleton<IMCPService, MCPService>();

// ==================== KAGGLE DATA LOADER ====================

var kaggleDatasetPath = configuration["Kaggle:DatasetPath"] ?? "Data/cards.csv";

builder.Services.AddSingleton<IKaggleDataLoader>(sp =>
{
    var persistenceManager = sp.GetRequiredService<PersistenceManager>();
    return new KaggleDataLoader(persistenceManager, kaggleDatasetPath);
});

// ==================== CORS (para frontends) ====================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==================== BUILD APP ====================

var app = builder.Build();

// ==================== MIDDLEWARE PIPELINE ====================

// Development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Proyecto Final API v1");
        options.RoutePrefix = string.Empty; // Swagger en la raíz
    });
}

// HTTPS redirection
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowAll");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// ==================== STARTUP INFO ====================

Console.WriteLine("====================================");
Console.WriteLine("Proyecto Final API - Starting...");
Console.WriteLine("====================================");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Persistence: {persistenceMode}");
Console.WriteLine($"JWT Issuer: {jwtIssuer}");
Console.WriteLine($"JWT Audience: {jwtAudience}");
Console.WriteLine($"Kaggle Dataset: {kaggleDatasetPath}");
Console.WriteLine($"Swagger UI: {(app.Environment.IsDevelopment() ? "http://localhost:5000" : "Disabled")}");
Console.WriteLine("====================================");

app.Run();
