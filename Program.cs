using Microsoft.EntityFrameworkCore;
using Npgsql;
using SoporteColegio.Data;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
else
{
    var urls = builder.Configuration["urls"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    builder.WebHost.UseUrls(string.IsNullOrWhiteSpace(urls) ? "http://localhost:5000" : urls);
}

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(ToNpgsqlConnectionString(databaseUrl)));
}
else if (builder.Environment.IsProduction())
{
    throw new InvalidOperationException("DATABASE_URL debe apuntar a una base PostgreSQL persistente en producción.");
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite("Data Source=soporte.db"));
}

var technicianUsername = builder.Configuration["TECHNICIAN_USERNAME"];
var technicianPassword = builder.Configuration["TECHNICIAN_PASSWORD"];
if (builder.Environment.IsProduction() &&
    (string.IsNullOrWhiteSpace(technicianUsername) || string.IsNullOrWhiteSpace(technicianPassword)))
{
    throw new InvalidOperationException("TECHNICIAN_USERNAME y TECHNICIAN_PASSWORD son obligatorios en producción.");
}

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

        logger.LogError(exception, "Error no controlado en la aplicación. Path: {Path}", context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("Ocurrió un error inesperado. El equipo de soporte ha sido notificado.");
    });
});

app.Use(async (context, next) =>
{
    var isTechnicianRoute = context.Request.Path.StartsWithSegments("/Tecnico") ||
        context.Request.Path.StartsWithSegments("/notificaciones-soporte") ||
        context.Request.Path.StartsWithSegments("/soporteHub");

    if (!app.Environment.IsProduction() || !isTechnicianRoute)
    {
        await next();
        return;
    }

    if (!HasValidTechnicianCredentials(context.Request, technicianUsername!, technicianPassword!))
    {
        context.Response.Headers.WWWAuthenticate = "Basic realm=\"Soporte Colegio Tecnico\", charset=\"UTF-8\"";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await next();
});

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<SoporteColegio.Data.ApplicationDbContext>();
	db.Database.EnsureCreated();
}

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Profesor}/{action=Index}/{id?}");
app.MapHub<SoporteColegio.Hubs.SoporteHub>("/notificaciones-soporte");
app.MapGet("/healthz", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

app.Run();

static string ToNpgsqlConnectionString(string databaseUrl)
{
    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
        (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
    {
        return databaseUrl;
    }

    var userInfo = uri.UserInfo.Split(':', 2);
    if (userInfo.Length != 2)
    {
        throw new InvalidOperationException("DATABASE_URL no contiene usuario y contraseña PostgreSQL válidos.");
    }

    var connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = Uri.UnescapeDataString(userInfo[1]),
        SslMode = SslMode.Require
    };

    return connectionString.ConnectionString;
}

static bool HasValidTechnicianCredentials(HttpRequest request, string username, string password)
{
    if (!AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var authorization) ||
        !string.Equals(authorization.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrWhiteSpace(authorization.Parameter))
    {
        return false;
    }

    try
    {
        var suppliedCredentials = Convert.FromBase64String(authorization.Parameter);
        var expectedCredentials = Encoding.UTF8.GetBytes($"{username}:{password}");
        return CryptographicOperations.FixedTimeEquals(suppliedCredentials, expectedCredentials);
    }
    catch (FormatException)
    {
        return false;
    }
}
