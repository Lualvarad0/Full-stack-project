using Back.Helpers;
using Back.Models;

namespace Back.Pagos
{
    /// <summary>
    /// Qué incluye cada plan. El plan Personal es para registrar a mano tus cuentas del mes frente a lo que ganas
    /// (Mis cuentas). Subir facturas o traerlas del SRI, ingresos, clientes, filtros y facturación son de Profesional.
    /// </summary>
    public static class AccesoPlan
    {
        /// <summary>Comprobantes que el plan Personal puede subir al mes: ninguno (lo dice el uso del plan).</summary>
        public const int LimiteComprobantesPersonal = 0;
        public const int MesesHistorialPersonal = 3;

        /// <summary>Facturas electrónicas (tipo 01) que cada RUC puede emitir en un mes (Profesional y Contadores).</summary>
        public const int LimiteFacturasMes = 300;

        /// <summary>Contribuyentes que un contador puede tener en su cartera (activos + solicitudes vigentes).</summary>
        public const int LimiteContribuyentesCartera = 10;

        public const string MensajeSubirFacturas =
            "Subir facturas o traerlas del SRI es parte del plan Profesional. En el plan gratuito registras tus cuentas a mano en Mis cuentas.";

        /// <summary>El administrador (dueño de la instalación) tiene todo sin pagar.</summary>
        public static bool TieneProfesional(Usuario usuario) =>
            usuario.AccesoContador || usuario.Rol == Roles.Admin || usuario.Plan != PlanSuscripcion.Personal;

        public static void ExigirProfesional(Usuario usuario, string seccion)
        {
            if (!TieneProfesional(usuario))
                throw new BusinessException($"{seccion} son parte del plan Profesional. Pruébalo {CatalogoPlanes.DiasDePrueba} días gratis.", StatusCodes.Status403Forbidden);
        }

        /// <summary>Primer día visible en el plan Personal (el mes actual y los dos anteriores).</summary>
        public static DateOnly InicioHistorial()
        {
            var hoy = HoyEcuador();
            return new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-(MesesHistorialPersonal - 1));
        }

        /// <summary>Inicio del mes en curso (hora de Ecuador) en UTC, para contar lo importado este mes.</summary>
        public static DateTime InicioMesUtc()
        {
            var hoy = HoyEcuador();
            return DateTime.SpecifyKind(new DateTime(hoy.Year, hoy.Month, 1).AddHours(5), DateTimeKind.Utc);
        }

        /// <summary>En el plan Personal no se puede consultar un mes anterior al historial incluido.</summary>
        public static void ValidarPeriodo(Usuario usuario, int? anio, int? mes)
        {
            if (TieneProfesional(usuario) || anio is null || mes is null) return;
            if (new DateOnly(anio.Value, mes.Value, 1) < InicioHistorial())
                throw new BusinessException(
                    $"El plan Personal muestra los últimos {MesesHistorialPersonal} meses. Con Profesional ves todo tu historial.", StatusCodes.Status403Forbidden);
        }

        private static DateOnly HoyEcuador() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
    }
}
