namespace SoporteColegio.Services;

public static class TicketNotificationService
{
    public static string BuildTeacherMessage(string? estado)
    {
        var normalized = (estado ?? string.Empty).Trim();

        return normalized switch
        {
            "Pendiente" => "Su aviso sigue pendiente. El equipo de soporte aún no lo ha revisado.",
            "En proceso" => "Su aviso fue recibido por el equipo de soporte y ya está en proceso.",
            "Resuelto" => "Su aviso ha sido resuelto por el equipo de soporte.",
            _ => "El estado de su aviso ha cambiado."
        };
    }
}
