# ProyectoFinal-EV1-BigData

Proyecto Final - Evaluacion 1 | Big Data & AI

## Descripcion

Aplicacion integral que implementa:
- API REST autenticada con JWT
- Persistencia dual (MySQL + Memoria con LINQ)
- Protocolo MCP para consultas con IA
- Interfaces: Consola, WPF y .NET MAUI

## Estructura del Proyecto

```
ProyectoFinal-EV1-BigData/
|
|-- src/
|   |-- Backend/
|   |   |-- API/
|   |   |   |-- Controllers/        # Controladores REST
|   |   |   |-- Models/             # Entidades y DTOs
|   |   |   |-- Services/           # Logica de negocio
|   |   |   |-- Auth/               # Autenticacion JWT
|   |   |   |-- Program.cs
|   |   |   |-- appsettings.json
|   |   |
|   |   |-- Persistence/
|   |   |   |-- Interfaces/         # IRepository (Open/Close)
|   |   |   |-- MySQL/              # Implementacion MySQL
|   |   |   |-- Memory/             # Implementacion LINQ
|   |   |
|   |   |-- MCP/
|   |       |-- Server/             # Servidor MCP
|   |       |-- Client/             # Cliente MCP
|   |       |-- Routers/
|   |           |-- RuleRouter.cs   # Enrutador manual
|   |           |-- LLMRouter.cs    # Enrutador LLM
|   |
|   |-- Frontend/
|       |-- ConsoleApp/             # Aplicacion Consola
|       |-- WPFApp/                 # Aplicacion WPF (MDI)
|       |-- MAUIApp/                # Aplicacion .NET MAUI
|
|-- data/
|   |-- kaggle/                     # Dataset de Kaggle
|   |-- scripts/                    # Scripts SQL
|
|-- config/
|   |-- App.config                  # Configuracion aplicacion
|
|-- docs/
|   |-- README.md
|   |-- API.md                      # Documentacion API
|
|-- tests/                          # Pruebas unitarias
|
|-- ProyectoFinal.sln               # Solucion Visual Studio
```

## Requisitos

- .NET 8.0 SDK
- MySQL Server 8.0+
- Visual Studio 2022 / Rider
- API Key de OpenAI/Gemini (para MCP)

## Configuracion

1. Clonar repositorio
2. Configurar `config/App.config` con cadena de conexion
3. Importar dataset de Kaggle en `data/kaggle/`
4. Ejecutar scripts SQL en `data/scripts/`

## Componentes

### Backend

| Componente | Descripcion |
|------------|-------------|
| API REST | Endpoints CRUD con autenticacion JWT |
| Persistencia | Patron Open/Close: MySQL y Memoria |
| MCP | Consultas en lenguaje natural con IA |

### Frontend

| Interfaz | Caracteristicas |
|----------|----------------|
| Consola | Login, CRUD, Seleccion persistencia |
| WPF | MDI, Login, Config, Master/Detail, MCP |
| MAUI | Navegacion, Login, Master/Detail, MCP |

## Rubrica

- [1 pt] API REST con JWT
- [2 pt] Persistencia (LINQ + MySQL + Config + OpenClose)
- [2 pt] MCP (Enrutador reglas + LLM)
- [4 pt] Interfaces (Consola + WPF + MAUI + Validacion)
- [1 pt] Rendimiento (async/await, Lazy Loading)

## Autor

Proyecto academico - FP Big Data & AI
