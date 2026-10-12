using Back.Helpers;
using Back.Models;

namespace Back.Pagos
{
    /// <summary>
    /// Qué incluye cada plan. El plan Personal es útil pero limitado (para invitar a pasar a Profesional):
    /// hasta <see cref="LimiteComprobantesPersonal"/> comprobantes nuevos por mes, historial de
    /// <see cref="MesesHistorialPersonal"/> meses y sin ingresos, clientes, filtros avanzados ni facturación.
    /// </summary>
    public static class AccesoPlan
    {
        public const int LimiteComprobantesPersonal = 50;
        public const int MesesHistorialPersonal = 3;

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
