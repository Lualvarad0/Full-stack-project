using System.ComponentModel.DataAnnotations;

namespace Back.Seguridad;

/// <summary>Campos de texto comercial no admiten marcado ni caracteres de control. No es un filtro SQL.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class TextoPlanoAttribute : ValidationAttribute
{
    public TextoPlanoAttribute() : base("Usa texto plano, sin etiquetas ni caracteres de control.") { }
    public override bool IsValid(object? value) => value is null || value is string texto &&
        !texto.Any(c => c is '<' or '>' || char.IsControl(c) && c is not '\n' and not '\r' and not '\t');
}
