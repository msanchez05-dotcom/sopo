# SoporteColegio

Aplicación web ASP.NET Core para gestión de tickets y soporte de salas dentro del colegio.

## Tecnologías

- .NET 9 SDK
- ASP.NET Core MVC
- EF Core + SQLite
- SignalR
- Python 3.x (se incluye una demo simple del patrón MVC)

## Estructura principal

- `Program.cs`: arranque de la aplicación
- `Data/`: contexto de base de datos
- `controllers/`: controladores MVC
- `models/`: entidades del dominio
- `ViewModels/`: modelos para las vistas
- `views/`: vistas Razor
- `Hubs/`: SignalR hub
- `Migrations/`: migraciones de EF Core

## Requisitos

1. Instalar .NET 9 SDK
2. Tener Python 3 instalado si quieres ejecutar la demo de Python

## Ejecutar la app web principal

```powershell
cd "c:\Users\MAURICIO\Desktop\proyecto_salas"
dotnet restore
dotnet run --urls http://localhost:5000
```

Luego abre:

```text
http://localhost:5000
```

## Ejecutar la demo Python

```powershell
cd "c:\Users\MAURICIO\Desktop\proyecto_salas"
py main.py
```

## Dependencias

El proyecto .NET usa EF Core y SQLite, y se restauran con:

```powershell
dotnet restore
```

El archivo `requirements.txt` no tiene paquetes adicionales para la demo de Python, por lo que basta con tener Python instalado.

## Publicar para acceder desde fuera del colegio

La aplicación se ejecuta en Render y guarda sus tickets en PostgreSQL administrado por Neon. Tu computador no necesita permanecer encendido. El repositorio incluye `Dockerfile` y `render.yaml` para preparar el servicio de Render; Neon se configura por separado para evitar crear recursos o cargos sin tu autorización.

1. En Neon, crea un proyecto PostgreSQL nuevo. Elige una región cercana a la región que usarás en Render y una base vacía para esta aplicación.
2. En el panel de Neon, copia la cadena de conexión **pooled**. Mantén habilitado TLS (`sslmode=require`) y no compartas ni guardes esa cadena en Git: contiene la contraseña de la base.
3. Sube el repositorio a GitHub y, en Render, elige **New + > Blueprint** y conecta ese repositorio.
4. Durante la creación del Blueprint, ingresa la cadena de Neon en `DATABASE_URL`. Crea también valores nuevos para `TECHNICIAN_USERNAME` y `TECHNICIAN_PASSWORD`; guárdalos solo como variables de entorno privadas en Render.
5. Render construirá el contenedor y asignará una URL pública HTTPS. El formulario de profesores será público y `/Tecnico` y su conexión SignalR pedirán autenticación.

Render no desplegará hasta que se complete su configuración inicial y se ingrese la cadena de Neon. La primera vez que arranque contra una base/schema vacío, la app creará las tablas y datos iniciales.

Los tickets que ya existan en `soporte.db` permanecen en el computador y no se copian a Neon automáticamente. Si necesitas conservarlos en línea, expórtalos e impórtalos antes de usar la nueva instalación.

El servicio web gratuito de Render puede dormir tras 15 minutos sin tráfico; la siguiente visita podría tardar alrededor de un minuto. Neon tiene sus propios límites y políticas del plan elegido. Revisa las condiciones actuales de ambos servicios antes de usar datos importantes; para disponibilidad constante, respaldos o mayor capacidad podrían requerirse planes pagados.

## Nota importante

El README anterior estaba desactualizado y describía un proyecto Python puro, pero la aplicación funcional del repositorio es la versión .NET de ASP.NET Core.
