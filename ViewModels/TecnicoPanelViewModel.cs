using SoporteColegio.Models;

namespace SoporteColegio.ViewModels;

public class TecnicoPanelViewModel
{
    public IReadOnlyList<Ticket> Tickets { get; set; } = [];
    public string FiltroActual { get; set; } = "Todos";
    public string OrdenActual { get; set; } = "prioridad";
}

public class TecnicoHistorialViewModel
{
    public IReadOnlyList<Ticket> Tickets { get; set; } = [];
    public string Filtro { get; set; } = "Todos";
    public string? FechaDesde { get; set; }
    public string? FechaHasta { get; set; }
    public int? SalaId { get; set; }
    public IReadOnlyList<Sala> Salas { get; set; } = [];
}
