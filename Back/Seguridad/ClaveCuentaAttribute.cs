using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Back.Seguridad;

/// <summary>BCrypt solo procesa 72 bytes; impedir contraseñas truncadas silenciosamente.</summary>
public sealed class ClaveCuentaAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is string clave && clave.Length >= 8
        && Encoding.UTF8.GetByteCount(clave) <= 72;
    public override string FormatErrorMessage(string name) => "Usa al menos 8 caracteres y un máximo de 72 bytes para la contraseña.";
}

public sealed class ConfirmacionClaveAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var password = context.ObjectType.GetProperty("Password")?.GetValue(context.ObjectInstance) as string;
        return value is string confirmacion && confirmacion == password ? ValidationResult.Success : new ValidationResult("Las contraseñas no coinciden.");
    }
}
