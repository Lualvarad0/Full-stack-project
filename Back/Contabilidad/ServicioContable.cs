using Back.Data;
using Back.Helpers;
using Back.Pagos;
using Microsoft.EntityFrameworkCore;
namespace Back.Contabilidad;

public abstract class ServicioContable(AppDbContext db, ControlPeriodoService control)
{
    protected readonly AppDbContext Db = db;
    protected readonly ControlPeriodoService Control = control;
    protected async Task ExigirAccesoAsync(int usuarioId, CancellationToken ct) => AccesoPlan.ExigirProfesional(await Db.Usuarios.SingleAsync(u => u.Id == usuarioId, ct), "Las funciones contables");
    protected static void ImporteExacto(decimal importe) { if (decimal.Round(importe, 2) != importe) throw new BusinessException("Utiliza importes con un máximo de dos decimales."); }
    protected static void FechaRegistrable(DateOnly fecha) { if (fecha.Year < 2000 || fecha > DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5))) throw new BusinessException("Indica una fecha válida, sin anticipar operaciones futuras."); }
}
