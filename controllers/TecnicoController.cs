using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SoporteColegio.Data;
using SoporteColegio.Hubs;
using SoporteColegio.Models;
using SoporteColegio.Services;
using SoporteColegio.ViewModels;

namespace SoporteColegio.Controllers;

public class TecnicoController : Controller
{
    private static readonly string[] EstadosPermitidos = ["Pendiente", "En proceso", "Resuelto"];
    private readonly ApplicationDbContext context;
    private readonly IHubContext<SoporteHub> hubContext;

    public TecnicoController(ApplicationDbContext context, IHubContext<SoporteHub> hubContext)
    {
        this.context = context;
        this.hubContext = hubContext;
    }

    private static string NormalizarEstado(string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
        {
            return string.Empty;
        }

        var texto = estado.Trim();
        if (texto.Equals("En Proceso", StringComparison.OrdinalIgnoreCase))
        {
            return "En proceso";
        }

        return EstadosPermitidos.FirstOrDefault(item => item.Equals(texto, StringComparison.OrdinalIgnoreCase)) ?? texto;
    }

    private static int PrioridadEstado(string estado) => estado switch
    {
        "Pendiente" => 0,
        "En proceso" => 1,
        "Resuelto" => 2,
        _ => 99
    };

    private IQueryable<Ticket> OrdenarTickets(IQueryable<Ticket> query, string orden = "prioridad")
    {
        return orden switch
        {
            "fecha_asc" => query.OrderBy(ticket => ticket.FechaReporte),
            "fecha_desc" => query.OrderByDescending(ticket => ticket.FechaReporte),
            "estado" => query.OrderBy(ticket => PrioridadEstado(ticket.Estado)).ThenByDescending(ticket => ticket.FechaReporte),
            _ => query.OrderBy(ticket => PrioridadEstado(ticket.Estado)).ThenByDescending(ticket => ticket.FechaReporte)
        };
    }

    [HttpGet]
    public async Task<IActionResult> Index(string filtro = "Todos", string orden = "prioridad")
    {
        var query = context.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Sala)
            .Include(ticket => ticket.FallaComun)
            .AsQueryable();

        if (!string.Equals(filtro, "Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(ticket => ticket.Estado == filtro);
        }

        var tickets = await OrdenarTickets(query, orden).ToListAsync();

        return View(new TecnicoPanelViewModel
        {
            Tickets = tickets,
            FiltroActual = filtro,
            OrdenActual = orden
        });
    }

    [HttpGet]
    public async Task<IActionResult> Historial(string filtro = "Resuelto", string? fechaDesde = null, string? fechaHasta = null, int? salaId = null)
    {
        var query = context.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Sala)
            .Include(ticket => ticket.FallaComun)
            .Where(ticket => ticket.Estado == "Resuelto" || ticket.Estado == "En proceso");

        if (!string.IsNullOrWhiteSpace(filtro) && !string.Equals(filtro, "Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(ticket => ticket.Estado == filtro);
        }

        if (DateTime.TryParse(fechaDesde, out var desde))
        {
            query = query.Where(ticket => ticket.FechaReporte >= desde);
        }

        if (DateTime.TryParse(fechaHasta, out var hasta))
        {
            query = query.Where(ticket => ticket.FechaReporte <= hasta.AddDays(1).AddSeconds(-1));
        }

        if (salaId.HasValue)
        {
            query = query.Where(ticket => ticket.SalaId == salaId.Value);
        }

        var tickets = await OrdenarTickets(query, "fecha_desc").ToListAsync();
        var salas = await context.Salas.AsNoTracking().OrderBy(sala => sala.Nombre).ToListAsync();

        return View(new TecnicoHistorialViewModel
        {
            Tickets = tickets,
            Filtro = filtro,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            SalaId = salaId,
            Salas = salas
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportarCsv(string filtro = "Todos", string? fechaDesde = null, string? fechaHasta = null, int? salaId = null)
    {
        var query = context.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Sala)
            .Include(ticket => ticket.FallaComun)
            .AsQueryable();

        if (!string.Equals(filtro, "Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(ticket => ticket.Estado == filtro);
        }

        if (DateTime.TryParse(fechaDesde, out var desde))
        {
            query = query.Where(ticket => ticket.FechaReporte >= desde);
        }

        if (DateTime.TryParse(fechaHasta, out var hasta))
        {
            query = query.Where(ticket => ticket.FechaReporte <= hasta.AddDays(1).AddSeconds(-1));
        }

        if (salaId.HasValue)
        {
            query = query.Where(ticket => ticket.SalaId == salaId.Value);
        }

        var tickets = await OrdenarTickets(query, "fecha_desc").ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Id,Sala,Equipo,Problema,Detalle,Estado,Fecha");
        foreach (var ticket in tickets)
        {
            csv.AppendLine($"{ticket.Id}," +
                $"{EscapeCsv(ticket.Sala?.Nombre ?? "")}," +
                $"{EscapeCsv(ticket.FallaComun?.Equipo ?? "")}," +
                $"{EscapeCsv(ticket.FallaComun?.Descripcion ?? "")}," +
                $"{EscapeCsv(ticket.DetalleAdicional ?? "")}," +
                $"{EscapeCsv(ticket.Estado)}," +
                $"{ticket.FechaReporte:yyyy-MM-dd HH:mm}");
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"tickets-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarEstado(int id, string estado)
    {
        var estadoNormalizado = NormalizarEstado(estado);

        if (!EstadosPermitidos.Contains(estadoNormalizado, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Estado no valido.");
        }

        var ticket = await context.Tickets
            .Include(item => item.Sala)
            .Include(item => item.FallaComun)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        ticket.Estado = estadoNormalizado;
        await context.SaveChangesAsync();

        var payload = new
        {
            id = ticket.Id,
            estado = ticket.Estado,
            sala = ticket.Sala?.Nombre ?? "Sala no disponible",
            equipo = ticket.FallaComun?.Equipo ?? "Equipo",
            problema = ticket.FallaComun?.Descripcion ?? "Problema",
            detalle = ticket.DetalleAdicional,
            mensaje = TicketNotificationService.BuildTeacherMessage(ticket.Estado)
        };

        await hubContext.Clients.All.SendAsync("TicketActualizado", new { id = ticket.Id, estado = ticket.Estado });
        await hubContext.Clients.All.SendAsync("TicketEstadoActualizado", payload);

        if (estadoNormalizado.Equals("En proceso", StringComparison.OrdinalIgnoreCase))
        {
            await hubContext.Clients.All.SendAsync("TicketRecibido", payload);
        }

        return Ok(new { ticket.Id, ticket.Estado });
    }

    private static string EscapeCsv(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
