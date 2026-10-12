using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Back.Contabilidad;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Microsoft.EntityFrameworkCore;
namespace Back.Tributacion;

public record PreparacionRetencionesDto(int Anio, int Mes, int Meses, string Version, decimal IrEmitida, decimal IvaEmitida, decimal IrRecibida, decimal IvaRecibida, List<string> Pendientes);
public record PreparacionAtsDto(int Anio, int Mes, int Meses, string Version, List<DocumentoFiscalDto> Documentos, List<string> Pendientes);
public record RevisionTributariaDto(int Id, TipoPreparacion Tipo, int Anio, int Mes, int Meses, EstadoPreparacion Estado, DateTime CreadoEn, DateTime? RevisadoEn, DateTime? PresentadoEn, string? ReferenciaPresentacion, bool Desactualizada);
public class RevisionTributariaService(AppDbContext db, ControlPeriodoService control, PreparacionTributariaService preparacion, PerfilTributarioService perfil) : ServicioContable(db, control)
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    public async Task<PreparacionRetencionesDto> RetencionesAsync(int uid, int anio, int mes, int meses, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); var (desde, hasta) = PreparacionTributariaService.Periodo(anio, mes, meses);
        var ret = await Db.Set<RetencionSri>().AsNoTracking().Include(r => r.Detalles).Where(r => r.UsuarioId == uid && r.PeriodoFiscal >= desde && r.PeriodoFiscal < hasta).ToListAsync(ct);
        decimal Valor(Direccion direccion, string impuesto) => ret.Where(r => r.Direccion == direccion && r.RevisadoEn.HasValue).Sum(r => r.Detalles.Where(d => d.CodigoImpuesto == impuesto).Sum(d => d.Valor));
        var pendientes = new List<string>(); if (ret.Any(r => r.RevisadoEn == null)) pendientes.Add("Hay retenciones pendientes de revisión.");
        if ((await perfil.PerfilAsync(uid, ct)).ConfirmadoEn == null) pendientes.Add("Confirma las obligaciones del RUC.");
        return new(anio, mes, meses, ReglasTributarias.Version, Valor(Direccion.Emitido, "1"), Valor(Direccion.Emitido, "2"), Valor(Direccion.Recibido, "1"), Valor(Direccion.Recibido, "2"), pendientes);
    }
    public async Task<PreparacionAtsDto> AtsAsync(int uid, int anio, int mes, int meses, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); var docs = await preparacion.DocumentosAsync(uid, anio, mes, meses, ct);
        var pendientes = new List<string>();
        if ((await perfil.PerfilAsync(uid, ct)).ConfirmadoEn == null) pendientes.Add("Confirma las obligaciones del RUC.");
        foreach (var d in docs) {
            if (d.Finalidad == FinalidadComprobante.SinDefinir || d.Renta == TratamientoRentaDocumento.Pendiente) pendientes.Add($"{d.Numero}: revisa clasificación y tratamiento fiscal.");
            if (d.Direccion == Direccion.Recibido && string.IsNullOrEmpty(d.CodigoSustento)) pendientes.Add($"{d.Numero}: completa el código de sustento del catálogo ATS.");
            if (string.IsNullOrEmpty(d.FormaPago)) pendientes.Add($"{d.Numero}: completa la forma de pago SRI.");
        }
        // La base revisable conserva documentos electrónicos; no se presenta como XML ATS oficial.
        return new(anio, mes, meses, ReglasTributarias.Version, docs, pendientes);
    }
    public async Task<object> VistaAsync(int uid, TipoPreparacion tipo, int anio, int mes, int meses, CancellationToken ct) => tipo switch {
        TipoPreparacion.Iva => await preparacion.IvaAsync(uid, anio, mes, meses, ct),
        TipoPreparacion.Renta => await preparacion.RentaAsync(uid, anio, ct),
        TipoPreparacion.GastosPersonales => await preparacion.GastosAsync(uid, anio, ct),
        TipoPreparacion.Retenciones => await RetencionesAsync(uid, anio, mes, meses, ct),
        TipoPreparacion.Ats => await AtsAsync(uid, anio, mes, meses, ct), _ => throw new BusinessException("Tipo de preparación no reconocido.")
    };
    public async Task<int> RevisarAsync(int uid, RevisarTributoRequest r, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); PreparacionTributariaService.Periodo(r.Anio, r.Mes, r.Meses);
        if (!r.Confirmado) throw new BusinessException("Confirma que revisaste las bases y completaste los documentos externos que correspondan.");
        if (r.Tipo is TipoPreparacion.Renta or TipoPreparacion.GastosPersonales && (r.Mes != 1 || r.Meses != 12)) throw new BusinessException("Renta y gastos personales se revisan por ejercicio completo.");
        await using var tx = await Control.AbrirAsync(uid, [], ct);
        var vista = await VistaAsync(uid, r.Tipo, r.Anio, r.Mes, r.Meses, ct);
        if (Pendientes(vista).Count > 0) throw BusinessException.Conflict("Resuelve los pendientes antes de confirmar la revisión.");
        if ((await perfil.PerfilAsync(uid, ct)).ConfirmadoEn == null) throw BusinessException.Conflict("Confirma las obligaciones del RUC antes de guardar la revisión tributaria.");
        if (await Db.FacturasEmitidas.AnyAsync(f => f.UsuarioId == uid && f.FechaEmision.Year == r.Anio && (f.Estado == EstadoFactura.Firmada || f.Estado == EstadoFactura.Recibida || f.Estado == EstadoFactura.EnProceso), ct)) throw BusinessException.Conflict("Termina las autorizaciones SRI pendientes del ejercicio.");
        var revision = new RevisionTributaria { UsuarioId = uid, Tipo = r.Tipo, Anio = r.Anio, Mes = r.Mes, Meses = r.Meses,
            Estado = EstadoPreparacion.Revisado, RevisadoEn = DateTime.UtcNow, Contenido = JsonSerializer.Serialize(vista, Json), Huella = await HuellaAsync(uid, r.Tipo, r.Anio, r.Mes, r.Meses, vista, ct) };
        Db.Add(revision); Control.Auditar(uid, "RevisarPreparacionTributaria", "Declaracion", $"{r.Tipo}/{r.Anio}/{r.Mes}/{r.Meses}", "Revisión interna confirmada; no se envió al SRI.");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); return revision.Id;
    }
    public async Task PresentarManualAsync(int uid, int id, PresentacionManualRequest r, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); FechaRegistrable(r.Fecha); await using var tx = await Control.AbrirAsync(uid, [], ct);
        var revision = await Db.Set<RevisionTributaria>().SingleOrDefaultAsync(r => r.UsuarioId == uid && r.Id == id, ct) ?? throw BusinessException.NotFound("Preparación no encontrada.");
        if (revision.Estado != EstadoPreparacion.Revisado) throw BusinessException.Conflict("Esta revisión ya tiene registrada una presentación.");
        var (_, hasta) = PreparacionTributariaService.Periodo(revision.Anio, revision.Mes, revision.Meses);
        if (r.Fecha < hasta) throw new BusinessException("La fecha de presentación no puede ser anterior al cierre del período informado.");
        var actual = await VistaAsync(uid, revision.Tipo, revision.Anio, revision.Mes, revision.Meses, ct);
        if (revision.Huella != await HuellaAsync(uid, revision.Tipo, revision.Anio, revision.Mes, revision.Meses, actual, ct) || Pendientes(actual).Count > 0) throw BusinessException.Conflict("Las bases cambiaron desde la revisión. Revisa una nueva preparación antes de registrar su presentación.");
        revision.Estado = EstadoPreparacion.PresentadoManual; revision.PresentadoEn = r.Fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); revision.ReferenciaPresentacion = r.Referencia.Trim();
        Control.Auditar(uid, "RegistrarPresentacionManual", "Declaracion", id.ToString(), $"Referencia={r.Referencia.Trim()}; fecha={r.Fecha}. Informado por el usuario, sin confirmación automática del SRI.");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task<List<RevisionTributariaDto>> HistorialAsync(int uid, int anio, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); ReglasTributarias.Anio(anio);
        var registros = await Db.Set<RevisionTributaria>().AsNoTracking().Where(r => r.UsuarioId == uid && r.Anio == anio).OrderByDescending(r => r.Id).Take(100).ToListAsync(ct);
        var huellas = new Dictionary<string, string>(); var resultado = new List<RevisionTributariaDto>();
        foreach (var r in registros) {
            var clave = $"{r.Tipo}/{r.Mes}/{r.Meses}";
            if (!huellas.ContainsKey(clave)) { var actual = await VistaAsync(uid, r.Tipo, r.Anio, r.Mes, r.Meses, ct); huellas[clave] = await HuellaAsync(uid, r.Tipo, r.Anio, r.Mes, r.Meses, actual, ct); }
            resultado.Add(new(r.Id, r.Tipo, r.Anio, r.Mes, r.Meses, r.Estado, r.CreadoEn, r.RevisadoEn, r.PresentadoEn, r.ReferenciaPresentacion, r.Huella != huellas[clave]));
        }
        return resultado;
    }
    private async Task<string> HuellaAsync(int uid, TipoPreparacion tipo, int anio, int mes, int meses, object vista, CancellationToken ct)
    {
        var (desde, hasta) = PreparacionTributariaService.Periodo(anio, mes, meses);
        var fuentes = await Db.Comprobantes.AsNoTracking().Where(c => c.UsuarioId == uid && c.FechaEmision >= desde && c.FechaEmision < hasta)
            .OrderBy(c => c.Id).Select(c => new { c.Id, c.ClaveAcceso, c.RevisadoEn, c.FechaEmision, c.IdentificacionComprador, c.RucEmisor, c.Total, c.Finalidad, c.TratamientoIva }).ToListAsync(ct);
        var fiscales = await Db.Set<RevisionFiscalComprobante>().AsNoTracking().Where(r => r.UsuarioId == uid).OrderBy(r => r.ComprobanteId)
            .Select(r => new { r.ComprobanteId, r.Renta, r.CodigoSustento, r.FormaPago, r.ParteRelacionada, r.Observacion, r.RevisadoEn }).ToListAsync(ct);
        var cargas = await perfil.CargasAsync(uid, anio, ct);
        var obligaciones = await perfil.PerfilAsync(uid, ct);
        var retenciones = await Db.Set<RetencionSri>().AsNoTracking().Where(r => r.UsuarioId == uid && r.PeriodoFiscal.Year == anio).OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.ClaveAcceso, r.RevisadoEn }).ToListAsync(ct);
        var datos = JsonSerializer.Serialize(new { Tipo = tipo, Vista = vista, Fuentes = fuentes, Fiscales = fiscales, Cargas = cargas, Obligaciones = obligaciones, Retenciones = retenciones }, Json);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(datos)));
    }
    public static List<string> Pendientes(object vista) => vista switch {
        IvaPreparadoDto d => d.Pendientes, RentaPreparadaDto d => d.Pendientes, GastosPersonalesDto d => d.Pendientes,
        PreparacionRetencionesDto d => d.Pendientes, PreparacionAtsDto d => d.Pendientes, _ => ["Preparación no reconocida."]
    };
}
