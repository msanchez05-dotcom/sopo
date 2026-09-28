using System.ComponentModel.DataAnnotations;

namespace SoporteColegio.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SalaId { get; set; }
        public Sala? Sala { get; set; }

        [Required]
        public int FallaComunId { get; set; }
        public FallaComun? FallaComun { get; set; }

        public string? DetalleAdicional { get; set; } // Comentario libre opcional

        public DateTime FechaReporte { get; set; } = DateTime.UtcNow;

        public string Estado { get; set; } = "Pendiente"; // Pendiente, En Proceso, Resuelto
    }
}
