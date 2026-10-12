using System.Text.Json;
using Back.Contabilidad;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Microsoft.EntityFrameworkCore;
namespace Back.Tributacion;

public class PerfilTributarioService(AppDbContext db, ControlPeriodoService control) : ServicioContable(db, control)
{
    public async Task<List<CargaFamiliar>> CargasAsync(int uid, int anio, CancellationToken ct)
    {
        ReglasTributarias.Anio(anio);
        return await Db.Set<CargaFamiliar>().AsNoTracking().Where(c => c.UsuarioId == uid && c.Anio == anio).OrderBy(c => c.Nombre).ToListAsync(ct);
    }
    public async Task<int> CargaAsync(int uid, CargaFamiliarRequest r, CancellationToken ct)
    {
        ReglasTributarias.Anio(r.Anio); ImporteExacto(r.IngresosGravados);
        if (!r.DependenciaConfirmada || r.IngresosGravados > ReglasTributarias.Sbu) throw new BusinessException("Confirma la dependencia económica y que sus ingresos gravados anuales no superan un salario básico unificado.");
        if (r.FechaNacimiento.Year < 1900 || r.FechaNacimiento > new DateOnly(r.Anio, 12, 31)) throw new BusinessException("Revisa la fecha de nacimiento.");
        if (r.Parentesco == ParentescoCarga.Hijo && r.Anio - r.FechaNacimiento.Year > 21 && !r.Discapacidad) throw new BusinessException("Los hijos mayores de 21 años necesitan la condición de discapacidad para registrarse como carga.");
        var u = await Db.Usuarios.SingleAsync(u => u.Id == uid, ct);
        if (u.Identificaciones().Contains(r.Identificacion)) throw new BusinessException("El titular no se registra como su propia carga.");
        await using var tx = await Control.AbrirAsync(uid, Enumerable.Range(1, 12).Select(m => new DateOnly(r.Anio, m, 1)), ct);
        if (await Db.Set<CargaFamiliar>().AnyAsync(c => c.UsuarioId == uid && c.Anio == r.Anio && c.Identificacion == r.Identificacion, ct)) throw BusinessException.Conflict("La carga ya está registrada en este ejercicio.");
        if (await Db.Set<CargaFamiliar>().CountAsync(c => c.UsuarioId == uid && c.Anio == r.Anio, ct) >= 20) throw new BusinessException("Revisa las cargas registradas antes de añadir más.");
        var c = new CargaFamiliar { UsuarioId = uid, Anio = r.Anio, Identificacion = r.Identificacion, Nombre = r.Nombre.Trim(), Parentesco = r.Parentesco, FechaNacimiento = r.FechaNacimiento,
            IngresosGravados = r.IngresosGravados, Discapacidad = r.Discapacidad, EnfermedadCatastrofica = r.EnfermedadCatastrofica, DependenciaConfirmada = true };
        Db.Add(c); Control.Auditar(uid, "RegistrarCargaFamiliar", "Cargas", r.Anio.ToString(), "Dependencia y uso exclusivo declarados por el titular; pendiente de registro oficial en SRI.");
        u.CargasFamiliares = await Db.Set<CargaFamiliar>().CountAsync(c => c.UsuarioId == uid && c.Anio == r.Anio, ct) + 1;
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); return c.Id;
    }
    public async Task QuitarCargaAsync(int uid, int id, CancellationToken ct)
    {
        var c = await Db.Set<CargaFamiliar>().SingleOrDefaultAsync(c => c.UsuarioId == uid && c.Id == id, ct) ?? throw BusinessException.NotFound("Carga no encontrada.");
        await using var tx = await Control.AbrirAsync(uid, Enumerable.Range(1, 12).Select(m => new DateOnly(c.Anio, m, 1)), ct);
        var u = await Db.Usuarios.SingleAsync(u => u.Id == uid, ct);
        u.CargasFamiliares = await Db.Set<CargaFamiliar>().CountAsync(f => f.UsuarioId == uid && f.Anio == c.Anio, ct) - 1;
        Db.Remove(c); Control.Auditar(uid, "QuitarCargaFamiliar", "Cargas", c.Anio.ToString(), "Carga retirada del cálculo local."); await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task<PerfilObligaciones> PerfilAsync(int uid, CancellationToken ct)
    {
        var p = await Db.Set<PerfilObligaciones>().AsNoTracking().SingleOrDefaultAsync(p => p.UsuarioId == uid, ct);
        if (p is not null) return p;
        var u = await Db.Usuarios.SingleAsync(u => u.Id == uid, ct);
        return new() { UsuarioId = uid, Iva = u.Regimen == RegimenTributario.RimpeEmprendedor ? FrecuenciaObligacion.Semestral : u.Regimen == RegimenTributario.RimpeNegocioPopular ? FrecuenciaObligacion.Ninguna : FrecuenciaObligacion.Mensual };
    }
    public async Task GuardarPerfilAsync(int uid, PerfilObligacionesRequest r, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct);
        if (r.Feriados.Any(f => f.Year is < 2026 or > 2027)) throw new BusinessException("Registra feriados de 2026 y 2027 para el calendario de este ejercicio.");
        var u = await Db.Usuarios.SingleAsync(u => u.Id == uid, ct);
        if (r.Confirmado && (string.IsNullOrEmpty(u.Ruc) || !u.Regimen.HasValue)) throw new BusinessException("Completa tu RUC y régimen antes de confirmar las obligaciones.");
        await using var tx = await Control.AbrirAsync(uid, [], ct);
        var p = await Db.Set<PerfilObligaciones>().SingleOrDefaultAsync(p => p.UsuarioId == uid, ct);
        if (p is null) { p = new() { UsuarioId = uid }; Db.Add(p); }
        p.Iva = r.Iva; p.Retenciones = r.Retenciones; p.Ats = r.Ats; p.Renta = r.Renta; p.GastosPersonales = r.GastosPersonales;
        p.Sociedad = r.Sociedad; p.Especial = r.Especial; p.Galapagos = r.Galapagos; p.Actividades = r.Actividades.Trim();
        p.Feriados = JsonSerializer.Serialize(r.Feriados.Distinct().Order()); p.ConfirmadoEn = r.Confirmado ? DateTime.UtcNow : null;
        Control.Auditar(uid, "ActualizarObligaciones", "PerfilTributario", uid.ToString(), "Obligaciones y feriados informados; no representa una actualización del catastro SRI.");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task<List<VencimientoTributarioDto>> CalendarioAsync(int uid, int anio, CancellationToken ct)
    {
        ReglasTributarias.Anio(anio); await ExigirAccesoAsync(uid, ct);
        var u = await Db.Usuarios.SingleAsync(u => u.Id == uid, ct); var p = await PerfilAsync(uid, ct);
        if (u.Ruc?.Length != 13) throw new BusinessException("Completa el RUC para calcular los vencimientos.");
        var feriados = JsonSerializer.Deserialize<List<DateOnly>>(p.Feriados) ?? [];
        var revisiones = await Db.Set<RevisionTributaria>().AsNoTracking().Where(r => r.UsuarioId == uid && r.Anio == anio).OrderByDescending(r => r.Id).ToListAsync(ct);
        var resultado = new List<VencimientoTributarioDto>();
        var dia = p.Galapagos ? 28 : p.Especial ? 9 : u.Ruc[8] == '0' ? 28 : 8 + (u.Ruc[8] - '0') * 2;
        void Agregar(string nombre, TipoPreparacion tipo, int mes, int meses, DateOnly nominal) {
            var fecha = Ajustar(nominal, p.Especial, feriados);
            var revision = revisiones.FirstOrDefault(r => r.Tipo == tipo && r.Mes == mes && r.Meses == meses);
            resultado.Add(new(nombre, meses == 12 ? anio.ToString() : $"{anio}-{mes:D2}" + (meses == 6 ? $" a {mes + 5:D2}" : ""), fecha,
                revision?.Estado == EstadoPreparacion.PresentadoManual ? "Presentado (registro manual)" : p.ConfirmadoEn == null ? "Por confirmar con el RUC y feriados" : "Pendiente", revision?.Id));
        }
        void Periodica(string nombre, TipoPreparacion tipo, FrecuenciaObligacion frecuencia, int desfase) {
            if (frecuencia == FrecuenciaObligacion.Ninguna) return; var meses = frecuencia == FrecuenciaObligacion.Semestral ? 6 : 1;
            for (var m = 1; m <= 12; m += meses) { var vencimiento = new DateOnly(anio, m, 1).AddMonths(meses - 1 + desfase); Agregar(nombre, tipo, m, meses, new(vencimiento.Year, vencimiento.Month, dia)); }
        }
        Periodica("IVA", TipoPreparacion.Iva, p.Iva, 1);
        Periodica("Retenciones IR", TipoPreparacion.Retenciones, p.Retenciones, 1);
        Periodica("ATS", TipoPreparacion.Ats, p.Ats, 2);
        if (p.Renta) Agregar("Renta", TipoPreparacion.Renta, 1, 12, new(anio + 1, p.Sociedad ? 4 : u.Regimen is RegimenTributario.RimpeEmprendedor or RegimenTributario.RimpeNegocioPopular ? 5 : 3, dia));
        if (p.GastosPersonales) Agregar("Anexo de gastos personales", TipoPreparacion.GastosPersonales, 1, 12, new(anio + 1, 2, dia));
        return resultado.OrderBy(r => r.Fecha).ToList();
    }
    public static DateOnly Ajustar(DateOnly fecha, bool especial, IEnumerable<DateOnly> feriados)
    {
        var nominal = fecha; var noHabiles = feriados.ToHashSet();
        bool NoHabil(DateOnly f) => f.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || noHabiles.Contains(f);
        if (especial) { while (NoHabil(fecha)) fecha = fecha.AddDays(-1); return fecha; }
        while (NoHabil(fecha)) fecha = fecha.AddDays(1);
        if (fecha.Month != nominal.Month) { fecha = nominal; while (NoHabil(fecha)) fecha = fecha.AddDays(-1); }
        return fecha;
    }
}
