using System.Globalization;
using System.Text.Json;
using Back.Clasificacion;
using Back.Contabilidad;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Back.Sri;
using Back.Seguridad;
using Microsoft.EntityFrameworkCore;
namespace Back.Tributacion;

public class PreparacionTributariaService(AppDbContext db, ControlPeriodoService control, PerfilTributarioService perfil) : ServicioContable(db, control)
{
    private static readonly RentaRequest RentaVacia = new(0, 0, 0, 0, 0, 0, 0, 0, false, false, [], "");
    private static readonly IvaRequest IvaVacio = new(1, 0, 0, 0, "");
    public async Task<List<DocumentoFiscalDto>> DocumentosAsync(int uid, int anio, int mes, int meses, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); var (desde, hasta) = Periodo(anio, mes, meses);
        var c = await Documentos(uid, desde, hasta, ct); var revisiones = await Db.Set<RevisionFiscalComprobante>().Where(r => r.UsuarioId == uid).ToDictionaryAsync(r => r.ComprobanteId, ct);
        return c.Select(c => { var r = revisiones.GetValueOrDefault(c.Id); return new DocumentoFiscalDto(c.Id, c.FechaEmision, c.NumeroComprobante, c.Direccion == Direccion.Emitido ? c.RazonSocialComprador : c.RazonSocialEmisor,
            c.Direccion, c.Finalidad, c.Subtotal * c.Signo, c.Iva * c.Signo, r?.Renta ?? TratamientoRentaDocumento.Pendiente, r?.CodigoSustento ?? "", r?.FormaPago ?? "", r?.ParteRelacionada ?? false, r?.Observacion ?? ""); }).ToList();
    }
    public async Task RevisarDocumentoAsync(int uid, int id, RevisionFiscalRequest r, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); var c = await Db.Comprobantes.SingleOrDefaultAsync(c => c.UsuarioId == uid && c.Id == id, ct) ?? throw BusinessException.NotFound("Comprobante no encontrado.");
        await using var tx = await Control.AbrirAsync(uid, [c.FechaEmision], ct);
        if (c.Direccion == Direccion.Emitido && r.Renta is TratamientoRentaDocumento.Deducible or TratamientoRentaDocumento.DeducibleRimpe or TratamientoRentaDocumento.NoDeducible
            || c.Direccion == Direccion.Recibido && r.Renta is TratamientoRentaDocumento.GravadoGeneral or TratamientoRentaDocumento.GravadoRimpe or TratamientoRentaDocumento.Exento)
            throw new BusinessException("El tratamiento de renta no corresponde a la dirección del documento.");
        if (r.Renta is TratamientoRentaDocumento.Deducible or TratamientoRentaDocumento.DeducibleRimpe && c.Finalidad is not (FinalidadComprobante.Negocio or FinalidadComprobante.Mixto)) throw new BusinessException("Los rubros personales no se deducen como costos del negocio.");
        var registro = await Db.Set<RevisionFiscalComprobante>().SingleOrDefaultAsync(r => r.UsuarioId == uid && r.ComprobanteId == id, ct);
        if (registro is null) { registro = new() { ComprobanteId = id, UsuarioId = uid }; Db.Add(registro); }
        registro.Renta = r.Renta; registro.CodigoSustento = r.CodigoSustento ?? ""; registro.FormaPago = r.FormaPago ?? ""; registro.ParteRelacionada = r.ParteRelacionada; registro.Observacion = r.Observacion.Trim(); registro.RevisadoEn = DateTime.UtcNow;
        Control.Auditar(uid, "RevisarTratamientoFiscal", "Comprobante", id.ToString(), $"Renta={r.Renta}; sustento={r.CodigoSustento}; pago={r.FormaPago}");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task GuardarRentaAsync(int uid, int anio, RentaRequest r, CancellationToken ct)
    {
        ReglasTributarias.Anio(anio); await ExigirAccesoAsync(uid, ct);
        if (r.IessPersonal > r.SueldosGravados) throw new BusinessException("El aporte personal al IESS no puede superar los sueldos gravados.");
        if (r.Proyeccion.Select(p => p.Categoria).Distinct().Count() != r.Proyeccion.Count || r.Proyeccion.Any(p => !ImportesGastosPersonales.EsRubro(p.Categoria))) throw new BusinessException("Registra una proyección por cada rubro personal válido.");
        foreach (var valor in new[] { r.SueldosGravados, r.IessPersonal, r.RetencionesSueldo, r.OtrosIngresosGeneral, r.OtrosGastosGeneral, r.IngresosRimpeAjuste, r.Exoneraciones, r.AnticiposRenta }.Concat(r.Proyeccion.Select(p => p.Importe))) ImporteExacto(valor);
        if ((r.OtrosIngresosGeneral != 0 || r.OtrosGastosGeneral != 0 || r.IngresosRimpeAjuste != 0 || r.Exoneraciones != 0 || r.AnticiposRenta != 0) && r.SustentoAjustes.Trim().Length < 10) throw new BusinessException("Documenta el sustento de los ajustes y valores externos.");
        await using var tx = await Control.AbrirAsync(uid, [], ct);
        var p = await Db.Set<PreparacionRenta>().SingleOrDefaultAsync(p => p.UsuarioId == uid && p.Anio == anio, ct);
        if (p is null) { p = new() { UsuarioId = uid, Anio = anio }; Db.Add(p); }
        p.SueldosGravados = r.SueldosGravados; p.IessPersonal = r.IessPersonal; p.RetencionesSueldo = r.RetencionesSueldo;
        p.OtrosIngresosGeneral = r.OtrosIngresosGeneral; p.OtrosGastosGeneral = r.OtrosGastosGeneral; p.IngresosRimpeAjuste = r.IngresosRimpeAjuste;
        p.Exoneraciones = r.Exoneraciones; p.AnticiposRenta = r.AnticiposRenta; p.BeneficioEspecial = r.BeneficioEspecial; p.Galapagos = r.Galapagos;
        p.Proyeccion = JsonSerializer.Serialize(r.Proyeccion); p.SustentoAjustes = r.SustentoAjustes.Trim();
        Control.Auditar(uid, "GuardarParametrosRenta", "Renta", anio.ToString(), "Valores externos y proyección; requiere revisión final.");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task GuardarIvaAsync(int uid, int anio, int mes, int meses, IvaRequest r, CancellationToken ct)
    {
        Periodo(anio, mes, meses); await ExigirAccesoAsync(uid, ct);
        if (decimal.Round(r.FactorCredito, 4) != r.FactorCredito) throw new BusinessException("El factor de crédito admite cuatro decimales.");
        foreach (var valor in new[] { r.CreditoAnterior, r.AjusteDebito, r.AjusteCredito }) ImporteExacto(valor);
        if ((r.CreditoAnterior != 0 || r.AjusteDebito != 0 || r.AjusteCredito != 0 || r.FactorCredito != 1) && r.SustentoAjustes.Trim().Length < 10) throw new BusinessException("Documenta el saldo anterior, factor y ajustes antes de guardarlos.");
        await using var tx = await Control.AbrirAsync(uid, [], ct);
        var p = await Db.Set<PreparacionIva>().SingleOrDefaultAsync(p => p.UsuarioId == uid && p.Anio == anio && p.Mes == mes && p.Meses == meses, ct);
        if (p is null) { p = new() { UsuarioId = uid, Anio = anio, Mes = mes, Meses = meses }; Db.Add(p); }
        p.FactorCredito = r.FactorCredito; p.CreditoAnterior = r.CreditoAnterior; p.AjusteDebito = r.AjusteDebito; p.AjusteCredito = r.AjusteCredito; p.SustentoAjustes = r.SustentoAjustes.Trim();
        Control.Auditar(uid, "GuardarParametrosIva", "IVA", $"{anio}-{mes:D2}/{meses}", "Saldo anterior, factor y ajustes guardados con sustento.");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task<GastosPersonalesDto> GastosAsync(int uid, int anio, CancellationToken ct)
    {
        ReglasTributarias.Anio(anio); var u = await Db.Usuarios.SingleAsync(u => u.Id == uid, ct);
        var cargas = await perfil.CargasAsync(uid, anio, ct); var parametros = await ParametrosRenta(uid, anio, ct);
        var ids = u.Identificaciones().Concat(cargas.Select(c => c.Identificacion)).ToHashSet();
        var docs = await Documentos(uid, new(anio, 1, 1), new(anio + 1, 1, 1), ct);
        var pendientes = new List<string>();
        if (!ids.Any()) pendientes.Add("Completa la identificación del titular para revisar a nombre de quién están las facturas.");
        var personales = docs.Where(c => c.Direccion == Direccion.Recibido && c.Finalidad is FinalidadComprobante.Personal or FinalidadComprobante.Mixto).ToList();
        var ajenos = personales.Count(c => !ids.Contains(c.IdentificacionComprador));
        if (ajenos > 0) pendientes.Add($"{ajenos} documentos personales están a nombre de una identificación no registrada como titular o carga; se excluyen de las bases.");
        var sinRevisar = docs.Count(c => c.Direccion == Direccion.Recibido && c.RevisadoEn == null);
        if (sinRevisar > 0) pendientes.Add($"Hay {sinRevisar} gastos sin finalidad revisada.");
        if (u.CargasFamiliares != cargas.Count) pendientes.Add("El número orientativo del perfil difiere de las cargas registradas para este año. El cálculo usa las cargas identificadas.");
        var rubros = Enum.GetValues<CategoriaGasto>().Where(c => ImportesGastosPersonales.EsRubro(c)).Select(categoria => {
            var total = personales.Where(c => ids.Contains(c.IdentificacionComprador)).Sum(c => c.Finalidad == FinalidadComprobante.Mixto
                ? c.Detalles.Where(d => d.Finalidad == FinalidadComprobante.Personal && d.Categoria == categoria && d.RevisadoEn.HasValue).Sum(d => d.Subtotal) * c.Signo
                : c.Categoria == categoria && c.RevisadoEn.HasValue ? c.Subtotal * c.Signo : 0);
            return new RubroGastoDto(categoria, total, parametros.Proyeccion.SingleOrDefault(p => p.Categoria == categoria)?.Importe ?? 0);
        }).ToList();
        var especial = parametros.BeneficioEspecial || u.EnfermedadCatastrofica || cargas.Any(c => c.Discapacidad || c.EnfermedadCatastrofica);
        var canastas = ReglasTributarias.Canastas(cargas.Count, especial); var canasta = ReglasTributarias.Canasta * (parametros.Galapagos ? 1.803m : 1);
        var tope = ReglasTributarias.Dinero(canasta * canastas); var real = Math.Max(0, rubros.Sum(r => r.Real)); var proyectado = rubros.Sum(r => r.Proyectado);
        return new(anio, ReglasTributarias.Version, cargas.Count, canasta, canastas, tope, real, proyectado, ReglasTributarias.Dinero(Math.Min(real, tope) * .18m), ReglasTributarias.Dinero(Math.Min(proyectado, tope) * .18m), rubros, pendientes);
    }
    public async Task<RentaPreparadaDto> RentaAsync(int uid, int anio, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); ReglasTributarias.Anio(anio); var u = await Db.Usuarios.SingleAsync(u => u.Id == uid, ct);
        var docs = await Documentos(uid, new(anio, 1, 1), new(anio + 1, 1, 1), ct); var revisiones = await Db.Set<RevisionFiscalComprobante>().Where(r => r.UsuarioId == uid).ToDictionaryAsync(r => r.ComprobanteId, ct);
        var p = await ParametrosRenta(uid, anio, ct); var gp = await GastosAsync(uid, anio, ct); var pendientes = gp.Pendientes.ToList();
        var negocio = docs.Where(c => c.Direccion == Direccion.Emitido || c.Finalidad is FinalidadComprobante.Negocio or FinalidadComprobante.Mixto).ToList();
        var sinRevisar = negocio.Count(c => !revisiones.ContainsKey(c.Id) || revisiones[c.Id].Renta == TratamientoRentaDocumento.Pendiente || c.RevisadoEn == null);
        if (sinRevisar > 0) pendientes.Add($"Hay {sinRevisar} documentos del negocio sin tratamiento fiscal confirmado.");
        if (negocio.Any(c => c.Direccion == Direccion.Recibido && (c.Finalidad == FinalidadComprobante.Negocio && c.TratamientoIva == TratamientoIva.Pendiente
            || c.Finalidad == FinalidadComprobante.Mixto && c.Detalles.Any(d => d.RevisadoEn == null || d.Finalidad == FinalidadComprobante.Negocio && (!d.Iva.HasValue || d.TratamientoIva == TratamientoIva.Pendiente))))) pendientes.Add("Revisa las partidas del negocio y su tratamiento de IVA antes de liquidar renta.");
        if (!u.Regimen.HasValue) pendientes.Add("Completa el régimen tributario del RUC.");
        if ((await perfil.PerfilAsync(uid, ct)).Sociedad) pendientes.Add("Esta liquidación aplica a personas naturales. Para sociedades, exporta las bases y realiza la conciliación fiscal con tu contador.");
        decimal Ingresos(TratamientoRentaDocumento t) => docs.Where(c => c.Direccion == Direccion.Emitido && revisiones.GetValueOrDefault(c.Id)?.Renta == t).Sum(c => c.Subtotal * c.Signo);
        var generales = Ingresos(TratamientoRentaDocumento.GravadoGeneral);
        var pagos = await Db.Set<PagoComprobante>().AsNoTracking().Where(p => p.UsuarioId == uid && p.AnuladoEn == null && p.Fecha.Year == anio).ToListAsync(ct);
        var documentosRimpe = await Db.Comprobantes.AsNoTracking().Where(c => c.UsuarioId == uid && c.Direccion == Direccion.Emitido && c.TipoComprobante == "01"
            && Db.Set<RevisionFiscalComprobante>().Any(r => r.UsuarioId == uid && r.ComprobanteId == c.Id && r.Renta == TratamientoRentaDocumento.GravadoRimpe)).ToDictionaryAsync(c => c.Id, ct);
        var rimpe = ReglasTributarias.Dinero(pagos.Where(p => documentosRimpe.ContainsKey(p.ComprobanteId)).Sum(p => {
            var c = documentosRimpe[p.ComprobanteId]; return c.Total > 0 ? p.Importe * c.Subtotal / c.Total * (p.Tipo == TipoAplicacionPago.Reembolso ? -1 : 1) : 0;
        })) + p.IngresosRimpeAjuste;
        var gastos = docs.Where(c => c.Direccion == Direccion.Recibido && revisiones.GetValueOrDefault(c.Id)?.Renta == TratamientoRentaDocumento.Deducible).Sum(c => {
            var credito = c.Finalidad == FinalidadComprobante.Negocio && c.TratamientoIva == TratamientoIva.ConCredito ? c.Iva : 0;
            return c.Finalidad == FinalidadComprobante.Mixto ? c.Detalles.Where(d => d.Finalidad == FinalidadComprobante.Negocio && d.RevisadoEn.HasValue).Sum(d => d.Subtotal + (d.TratamientoIva == TratamientoIva.ConCredito ? 0 : d.Iva ?? 0)) * c.Signo
                : c.Finalidad == FinalidadComprobante.Negocio ? (c.Subtotal + c.Iva - credito + c.OtrosImpuestos) * c.Signo : 0;
        });
        if (u.Regimen == RegimenTributario.General && documentosRimpe.Any()) pendientes.Add("Existen ingresos marcados RIMPE, aunque el perfil indica régimen general.");
        var baseGeneral = Math.Max(0, generales + p.SueldosGravados + p.OtrosIngresosGeneral - p.IessPersonal - gastos - p.OtrosGastosGeneral - p.Exoneraciones);
        var impuestoGeneral = ReglasTributarias.ImpuestoGeneral(baseGeneral);
        var impuestoRimpe = rimpe is >= 0 and <= 300000 ? ReglasTributarias.ImpuestoRimpe(rimpe) : 0;
        if (rimpe < 0 || rimpe > 300000) pendientes.Add("Los ingresos RIMPE quedan fuera de los rangos soportados; revisa el régimen y los ajustes.");
        // La rebaja personal se aplica solamente a la renta general, nunca al impuesto RIMPE.
        var rebaja = Math.Min(impuestoGeneral, gp.RebajaPotencial);
        var ret = await Retenciones(uid, new(anio, 1, 1), new(anio + 1, 1, 1), ct);
        rimpe += ReglasTributarias.Dinero(ret.Where(r => r.Direccion == Direccion.Recibido && r.RevisadoEn.HasValue).Sum(r => r.Detalles.Where(d => d.ComprobanteId.HasValue && documentosRimpe.ContainsKey(d.ComprobanteId.Value) && d.CodigoImpuesto is "1" or "2").Sum(d => {
            var c = documentosRimpe[d.ComprobanteId!.Value]; return c.Total > 0 ? d.Valor * c.Subtotal / c.Total : 0;
        })));
        impuestoRimpe = rimpe is >= 0 and <= 300000 ? ReglasTributarias.ImpuestoRimpe(rimpe) : 0;
        if (ret.Any(r => r.RevisadoEn == null)) pendientes.Add("Revisa las retenciones importadas antes de confirmar la preparación.");
        var retenido = ret.Where(r => r.Direccion == Direccion.Recibido && r.RevisadoEn.HasValue).Sum(r => r.Detalles.Where(d => d.CodigoImpuesto == "1").Sum(d => d.Valor)) + p.RetencionesSueldo;
        var saldo = impuestoGeneral + impuestoRimpe - rebaja - retenido - p.AnticiposRenta;
        return new(anio, ReglasTributarias.Version, u.Regimen, docs.Where(c => c.Direccion == Direccion.Emitido).Sum(c => c.Subtotal * c.Signo), gastos, generales + p.SueldosGravados + p.OtrosIngresosGeneral,
            baseGeneral, rimpe, impuestoGeneral, impuestoRimpe, rebaja, retenido, p.AnticiposRenta, Math.Max(0, saldo), Math.Max(0, -saldo), gp, p, pendientes);
    }
    public async Task<IvaPreparadoDto> IvaAsync(int uid, int anio, int mes, int meses, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); var (desde, hasta) = Periodo(anio, mes, meses);
        var docs = await Documentos(uid, desde, hasta, ct); var p = await Db.Set<PreparacionIva>().AsNoTracking().SingleOrDefaultAsync(p => p.UsuarioId == uid && p.Anio == anio && p.Mes == mes && p.Meses == meses, ct);
        var parametros = p is null ? IvaVacio : new(p.FactorCredito, p.CreditoAnterior, p.AjusteDebito, p.AjusteCredito, p.SustentoAjustes);
        var pendientes = new List<string>();
        var grupos = new Dictionary<string, (decimal VBase, decimal VIva, decimal CBase, decimal CIva)>();
        decimal credito = 0;
        foreach (var c in docs)
        {
            if (c.RevisadoEn == null) pendientes.Add($"{c.NumeroComprobante}: confirma finalidad y clasificación.");
            if (c.Finalidad == FinalidadComprobante.Mixto && c.Detalles.Any(d => d.RevisadoEn == null)) pendientes.Add($"{c.NumeroComprobante}: revisa todas las partidas.");
            if (c.Direccion == Direccion.Recibido && c.Finalidad == FinalidadComprobante.Negocio) {
                if (c.TratamientoIva == TratamientoIva.Pendiente) pendientes.Add($"{c.NumeroComprobante}: confirma el crédito tributario de IVA.");
                if (c.TratamientoIva == TratamientoIva.ConCredito) credito += c.Iva * c.Signo;
            }
            if (c.Direccion == Direccion.Recibido && c.Finalidad == FinalidadComprobante.Mixto) {
                if (c.Detalles.Any(d => d.Finalidad == FinalidadComprobante.Negocio && (d.TratamientoIva == TratamientoIva.Pendiente || !d.Iva.HasValue))) pendientes.Add($"{c.NumeroComprobante}: falta IVA o tratamiento de una partida del negocio.");
                credito += c.Detalles.Where(d => d.Finalidad == FinalidadComprobante.Negocio && d.TratamientoIva == TratamientoIva.ConCredito && d.RevisadoEn.HasValue).Sum(d => d.Iva ?? 0) * c.Signo;
            }
            if (c.Xml is null) { pendientes.Add($"{c.NumeroComprobante}: falta XML original para revisar tarifas."); continue; }
            var xml = XmlSeguro.Documento(c.Xml.Contenido); var impuestos = xml.Descendants("totalImpuesto").Where(t => (string?)t.Element("codigo") == "2").ToList();
            if (impuestos.Count == 0 && c.Iva != 0) pendientes.Add($"{c.NumeroComprobante}: el XML no informa las bases por tarifa de IVA.");
            foreach (var t in impuestos) {
                var codigo = (string?)t.Element("codigoPorcentaje") ?? "Sin código";
                var g = grupos.GetValueOrDefault(codigo); var baseIva = Decimal((string?)t.Element("baseImponible")) * c.Signo; var iva = Decimal((string?)t.Element("valor")) * c.Signo;
                grupos[codigo] = c.Direccion == Direccion.Emitido ? (g.VBase + baseIva, g.VIva + iva, g.CBase, g.CIva) : (g.VBase, g.VIva, g.CBase + baseIva, g.CIva + iva);
            }
        }
        var ret = await Retenciones(uid, desde, hasta, ct); if (ret.Any(r => r.RevisadoEn == null)) pendientes.Add("Confirma las retenciones del período.");
        decimal ValorRetencion(Direccion direccion, string impuesto) => ret.Where(r => r.Direccion == direccion && r.RevisadoEn.HasValue).Sum(r => r.Detalles.Where(d => d.CodigoImpuesto == impuesto).Sum(d => d.Valor));
        var retenido = ValorRetencion(Direccion.Recibido, "2"); var retIva = ValorRetencion(Direccion.Emitido, "2"); var retRenta = ValorRetencion(Direccion.Emitido, "1");
        var debito = docs.Where(c => c.Direccion == Direccion.Emitido).Sum(c => c.Iva * c.Signo) + parametros.AjusteDebito;
        var aplicado = ReglasTributarias.Dinero(credito * parametros.FactorCredito) + parametros.CreditoAnterior + parametros.AjusteCredito;
        if (debito < 0 || aplicado < 0) pendientes.Add("El saldo de ajustes genera un débito o crédito negativo; revisa su sustento.");
        var saldo = debito - aplicado - retenido;
        var perfilActual = await perfil.PerfilAsync(uid, ct);
        if (perfilActual.ConfirmadoEn == null) pendientes.Add("Confirma las obligaciones tributarias con el RUC antes de revisar la preparación.");
        return new(anio, mes, meses, ReglasTributarias.Version, grupos.OrderBy(g => g.Key).Select(g => new TarifaIvaDto(NombreTarifa(g.Key), g.Value.VBase, g.Value.VIva, g.Value.CBase, g.Value.CIva)).ToList(),
            debito, credito, aplicado, retenido, retIva, retRenta, Math.Max(0, saldo), Math.Max(0, -saldo), parametros, pendientes.Distinct().ToList());
    }
    public async Task<RentaRequest> ParametrosRenta(int uid, int anio, CancellationToken ct)
    {
        var p = await Db.Set<PreparacionRenta>().AsNoTracking().SingleOrDefaultAsync(p => p.UsuarioId == uid && p.Anio == anio, ct);
        return p is null ? RentaVacia : new(p.SueldosGravados, p.IessPersonal, p.RetencionesSueldo, p.OtrosIngresosGeneral, p.OtrosGastosGeneral, p.IngresosRimpeAjuste, p.Exoneraciones, p.AnticiposRenta, p.BeneficioEspecial, p.Galapagos,
            JsonSerializer.Deserialize<List<RubroProyeccion>>(p.Proyeccion) ?? [], p.SustentoAjustes);
    }
    public static (DateOnly Desde, DateOnly Hasta) Periodo(int anio, int mes, int meses)
    {
        ReglasTributarias.Anio(anio); var desde = PeriodosService.Fecha(anio, mes);
        if (meses is not (1 or 6 or 12) || mes + meses > 13 || meses == 6 && mes is not (1 or 7) || meses == 12 && mes != 1) throw new BusinessException("Selecciona un mes, semestre o ejercicio completo.");
        return (desde, desde.AddMonths(meses));
    }
    private Task<List<Comprobante>> Documentos(int uid, DateOnly desde, DateOnly hasta, CancellationToken ct) => Db.Comprobantes.AsNoTracking().Include(c => c.Detalles).Include(c => c.Xml)
        .Where(c => c.UsuarioId == uid && c.FechaEmision >= desde && c.FechaEmision < hasta && c.ClaveAcceso.Length == 49 && c.ClaveAcceso.Substring(23, 1) == "2").OrderBy(c => c.Id).ToListAsync(ct);
    private Task<List<RetencionSri>> Retenciones(int uid, DateOnly desde, DateOnly hasta, CancellationToken ct) => Db.Set<RetencionSri>().AsNoTracking().Include(r => r.Detalles).Where(r => r.UsuarioId == uid && r.PeriodoFiscal >= desde && r.PeriodoFiscal < hasta).OrderBy(r => r.Id).ToListAsync(ct);
    private static decimal Decimal(string? valor) => decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : throw new BusinessException("Un XML contiene un importe tributario que no se puede interpretar.");
    private static string NombreTarifa(string codigo) => codigo switch { "0" => "0 %", "2" => "12 %", "3" => "14 %", "4" => "15 %", "5" => "5 %", "6" => "No objeto", "7" => "Exento", "8" => "8 %", _ => "Código SRI " + codigo };
}
