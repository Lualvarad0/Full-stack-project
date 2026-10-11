using Back.Models;

namespace Back.Dtos
{
    public record AvisoCuentaDto(int Id, DateTime Fecha, TipoAviso Tipo, string Titulo, string Detalle, string? Ip, string? Dispositivo, bool EnviadoPorCorreo)
    {
        public static AvisoCuentaDto From(AvisoCuenta a) => new(a.Id, a.Fecha, a.Tipo, a.Titulo, a.Detalle, a.Ip, a.Dispositivo, a.EnviadoPorCorreo);
    }
}
