using System.ComponentModel.DataAnnotations;
using Back.Models;

namespace Back.Dtos
{
    public record RegistrosMesDto(int Anio, int Mes, int Usuarios);

    /// <summary>Indicadores del negocio para el panel de superadministrador.</summary>
    public record MetricasAdminDto(
        int UsuariosTotales,
        int UsuariosSuspendidos,
        int NuevosUltimos30Dias,
        int ActivosUltimos30Dias,
        Dictionary<PlanSuscripcion, int> PorPlan,
        int SuscripcionesEnPrueba,
        int SuscripcionesActivas,
        int SuscripcionesConPagoPendiente,
        /// <summary>Ingreso mensual recurrente (planes anuales divididos para 12), sin IVA.</summary>
        decimal IngresoMensualRecurrente,
        decimal CobradoUltimos30Dias,
        int FacturasEmitidas,
        int FacturasAutorizadas,
        int ComprobantesImportados,
        int ComprobantesEsteMes,
        List<RegistrosMesDto> RegistrosPorMes);

    public record UsuarioAdminDto(
        int Id,
        string Nombre,
        string Correo,
        string Rol,
        PlanSuscripcion Plan,
        bool Activo,
        DateTime FechaCreacion,
        DateTime? UltimoIngresoEn,
        string? Ruc,
        EstadoSuscripcion? EstadoSuscripcion,
        int Comprobantes,
        int FacturasEmitidas);

    public record PagoAdminDto(int Id, DateTime Fecha, string Usuario, string Correo, PlanSuscripcion Plan, decimal Monto, bool Aprobado, string? Ticket, string? Detalle);

    public record EstadoSistemaDto(
        string Entorno,
        bool KushkiConfigurado,
        bool KushkiPruebas,
        bool KushkiWebhookConfigurado,
        bool SmtpConfigurado,
        string? DominioBuzon,
        bool DataProtectionPersistente,
        int MigracionesPendientes,
        DateTime HoraServidorUtc);

    public record CambiarPlanAdminRequest([Required] PlanSuscripcion Plan);

    public record CambiarEstadoAdminRequest(bool Activo);
}
