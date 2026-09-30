using SoporteColegio.Models;

namespace SoporteColegio.ViewModels;

public class TecnicoPanelViewModel
{
    public IReadOnlyList<Ticket> Tickets { get; set; } = [];
    public string FiltroActual { get; set; } = "Todos";
    public string OrdenActual { get; set; } = "urgencia";
    public int? SalaId { get; set; }
    public string? FechaDesde { get; set; }
    public string? FechaHasta { get; set; }
    public IReadOnlyList<Sala> Salas { get; set; } = [];
}

public class TecnicoHistorialViewModel
{
    public IReadOnlyList<Ticket> Tickets { get; set; } = [];
    public IReadOnlyList<TecnicoSalaResumen> ResumenSalas { get; set; } = [];
    public string Filtro { get; set; } = "Todos";
    public string? Busqueda { get; set; }
    public string? FechaDesde { get; set; }
    public string? FechaHasta { get; set; }
    public int? SalaId { get; set; }
    public IReadOnlyList<Sala> Salas { get; set; } = [];
}

public class TecnicoSalaResumen
{
    public string Sala { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Pendientes { get; set; }
    public int EnProceso { get; set; }
    public int Resueltos { get; set; }
}
