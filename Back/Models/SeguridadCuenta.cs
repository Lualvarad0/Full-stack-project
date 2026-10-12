namespace Back.Models;

public enum PropositoTokenCuenta { RecuperarClave, VerificarCorreo }
public class TokenCuenta
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Hash { get; set; } = "";
    public PropositoTokenCuenta Proposito { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEn { get; set; }
    public DateTime? UsadoEn { get; set; }
}
public class SesionCuenta
{
    public string Id { get; set; } = "";
    public int UsuarioId { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEn { get; set; }
    public DateTime? RevocadaEn { get; set; }
    public string? Dispositivo { get; set; }
}
/// <summary>Correo de acceso persistente y cifrado; nunca guardar enlaces de recuperación en texto plano.</summary>
public class CorreoAccesoPendiente
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string ContenidoCifrado { get; set; } = "";
    public DateTime ExpiraEn { get; set; }
    public DateTime ProximoIntentoEn { get; set; } = DateTime.UtcNow;
    public int Intentos { get; set; }
    public DateTime? EnviadoEn { get; set; }
}
