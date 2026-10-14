namespace Back.Helpers;

/// <summary>Nudo es solo para mayores de edad: una sola regla para registro, perfil, cuentas antiguas y altas del administrador.</summary>
public static class EdadMinima
{
    public const int Anios = 18;
    public const string Mensaje = "Debes ser mayor de edad para usar Nudo.";

    public static bool EsMayorDeEdad(DateOnly fechaNacimiento) => fechaNacimiento <= HoyEcuador().AddYears(-Anios);

    public static void Validar(DateOnly fechaNacimiento)
    {
        if (fechaNacimiento < new DateOnly(1900, 1, 1)) throw new BusinessException("Revisa tu fecha de nacimiento.");
        if (!EsMayorDeEdad(fechaNacimiento)) throw new BusinessException(Mensaje);
    }

    private static DateOnly HoyEcuador() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
}
