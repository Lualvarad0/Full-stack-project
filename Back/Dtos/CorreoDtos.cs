using Back.Models;

namespace Back.Dtos
{
    public record CorreoRecibidoDto(
        int Id,
        DateTime RecibidoEn,
        string Remitente,
        string Asunto,
        EstadoCorreo Estado,
        int ArchivosXml,
        int ClavesEncontradas,
        int Importados,
        int Ingresos,
        string? Detalle)
    {
        public static CorreoRecibidoDto From(CorreoRecibido c) => new(
            c.Id, c.RecibidoEn, c.Remitente, c.Asunto, c.Estado, c.ArchivosXml, c.ClavesEncontradas, c.Importados, c.Ingresos, c.Detalle);
    }

    /// <param name="Direccion">Null si el servidor aún no tiene dominio de correo entrante configurado.</param>
    public record BuzonDto(
        string Codigo,
        string? Direccion,
        bool Configurado,
        DateTime? UltimoCorreoEn,
        string? ConfirmacionGmailCodigo,
        string? ConfirmacionGmailEnlace,
        DateTime? ConfirmacionGmailEn,
        List<CorreoRecibidoDto> Recientes)
    {
        public static BuzonDto From(BuzonCorreo b, string? dominio, IEnumerable<CorreoRecibido> recientes) => new(
            b.Codigo,
            dominio is null ? null : $"{b.Codigo}@{dominio}",
            dominio is not null,
            b.UltimoCorreoEn,
            b.ConfirmacionGmailCodigo,
            b.ConfirmacionGmailEnlace,
            b.ConfirmacionGmailEn,
            recientes.Select(CorreoRecibidoDto.From).ToList());
    }
}
