# Arquitectura del Proyecto

## Descripcion General

Este proyecto implementa una arquitectura de capas con separacion de responsabilidades, siguiendo los principios SOLID y patrones de diseno empresariales.

## Diagrama de Arquitectura

```
+------------------+     +------------------+     +------------------+
|    Frontend      |     |     Backend      |     |   Persistencia   |
+------------------+     +------------------+     +------------------+
| - ConsoleApp     |     | - API REST       |     | - MySQL          |
| - WPF (MDI)      |<--->| - JWT Auth       |<--->| - Memory (LINQ)  |
| - .NET MAUI      |     | - MCP Protocol   |     |                  |
+------------------+     +------------------+     +------------------+
```

## Componentes Principales

### 1. Capa de Persistencia (Open/Close Pattern)

Implementa el patron Open/Close para permitir cambio dinamico entre sistemas de persistencia sin modificar codigo.

```csharp
IRepository<T>
    |-- MySQLRepository<T>    // Persistencia en MySQL
    |-- MemoryRepository<T>   // Persistencia en memoria con LINQ
```

**Configuracion via App.config:**
```xml
<appSettings>
    <add key="PersistenceSystem" value="MySQL" /> <!-- o "Memory" -->
</appSettings>
```

### 2. Protocolo MCP (Model Context Protocol)

Implementa un sistema de enrutamiento dual para consultas en lenguaje natural:

```
Consulta Usuario
       |
       v
+-------------+
| RuleRouter  |  <-- Ejecuta PRIMERO (reglas manuales)
+-------------+
       |
       | (si no hay coincidencia, retorna null)
       v
+-------------+
| LLMRouter   |  <-- Ejecuta como FALLBACK (consulta LLM)
+-------------+
       |
       v
   Respuesta
```

**Flujo de ejecucion:**
1. RuleRouter intenta resolver con reglas predefinidas
2. Si RuleRouter retorna null, LLMRouter consulta al LLM
3. El resultado se devuelve al cliente

### 3. Autenticacion JWT

- Generacion de tokens con algoritmo HS256
- Validacion de tokens en cada peticion
- Expiracion configurable

### 4. Frontends

#### ConsoleApp
- Menu basado en texto
- Seleccion de sistema de persistencia
- Operaciones CRUD

#### WPF (MDI)
- Interfaz Multiple Document Interface con TabControl
- Ventanas: Login, Configuracion, Master/Detail, MCP

#### .NET MAUI
- Navegacion basada en Shell
- **Lazy Loading** en listas (paginacion bajo demanda)
- Soporte multiplataforma

## Patrones de Diseno Utilizados

| Patron | Uso |
|--------|-----|
| Repository | Abstraccion de acceso a datos |
| Open/Close | Cambio dinamico de persistencia |
| Dependency Injection | Inyeccion de servicios |
| Strategy | Routers MCP intercambiables |
| Chain of Responsibility | Cadena RuleRouter -> LLMRouter |
| Lazy Loading | Carga bajo demanda en MAUI |

## Tecnologias

- **.NET 8** - Framework principal
- **MySQL** - Base de datos relacional
- **JWT** - Autenticacion
- **OpenAI/Azure** - LLM para MCP
- **WPF** - UI Desktop Windows
- **.NET MAUI** - UI Multiplataforma
