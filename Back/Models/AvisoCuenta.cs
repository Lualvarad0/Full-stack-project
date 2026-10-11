namespace Back.Models
{
    /// <summary>
    /// Registro de actividad de seguridad de la cuenta (ingresos y cambios de configuración).
    /// Se muestra en la app y, si hay SMTP configurado, se envía por correo al usuario.
    /// </summary>
    public class AvisoCuenta
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public TipoAviso Tipo { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public string? Ip { get; set; }
        public string? Dispositivo { get; set; }
        public bool EnviadoPorCorreo { get; set; }
    }

    public enum TipoAviso
    {
        Registro,
        Ingreso,
        PerfilActualizado,
        BuzonRegenerado,
        FirmaElectronica,
        DatosFacturacion,
        PlanCambiado,
    }
}
