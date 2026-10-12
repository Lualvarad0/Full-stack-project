using System.Text;
using Back.Contabilidad;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Back.Sri;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
namespace Back.Facturacion;

public class DocumentosComplementariosService(AppDbContext db, ControlPeriodoService control, FacturacionService facturacion, IDataProtectionProvider proteccion, IConfiguration config)
{
    public async Task<FacturaEmitidaDto> NotaCreditoAsync(int usuarioId, EmitirNotaCreditoRequest request, CancellationToken ct)
    {
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
        FacturaEmitida f;
        await using (var tx = await control.AbrirAsync(usuarioId, [fecha], ct))
        {
            var origen = await db.FacturasEmitidas.Include(f => f.Detalles).SingleOrDefaultAsync(f => f.UsuarioId == usuarioId && f.Id == request.FacturaId, ct) ?? throw BusinessException.NotFound("Factura original no encontrada.");
            if (origen.TipoDocumento != "01" || origen.Estado != EstadoFactura.Autorizada) throw BusinessException.Conflict("La nota de crédito requiere una factura autorizada.");
            if (origen.TipoIdentificacionComprador == "07" || origen.IdentificacionComprador == FacturaXml.ConsumidorFinal) throw BusinessException.Conflict("No se emiten notas de crédito para consumidor final.");
            if (fecha > origen.FechaEmision.AddMonths(12)) throw BusinessException.Conflict("La factura supera el plazo de doce meses para notas de crédito.");
            if (request.Detalles.Select(d => d.DetalleId).Distinct().Count() != request.Detalles.Count) throw new BusinessException("No repitas un concepto en la nota de crédito.");
            f = new FacturaEmitida { UsuarioId = usuarioId, TipoDocumento = "04", FechaEmision = fecha,
                DocumentoSustentoId = origen.Id, NumeroDocumentoSustento = origen.Numero, FechaDocumentoSustento = origen.FechaEmision, Motivo = request.Motivo.Trim(),
                TipoIdentificacionComprador = origen.TipoIdentificacionComprador, IdentificacionComprador = origen.IdentificacionComprador, RazonSocialComprador = origen.RazonSocialComprador,
                CorreoComprador = origen.CorreoComprador, DireccionComprador = origen.DireccionComprador, FormaPago = origen.FormaPago };
            foreach (var p in request.Detalles)
            {
                var d = origen.Detalles.SingleOrDefault(d => d.Id == p.DetalleId) ?? throw BusinessException.NotFound("Concepto no encontrado en la factura original.");
                var acreditada = await db.FacturasEmitidas.Where(x => x.UsuarioId == usuarioId && x.DocumentoSustentoId == origen.Id && x.TipoDocumento == "04"
                    && (x.Estado == EstadoFactura.Firmada || x.Estado == EstadoFactura.Recibida || x.Estado == EstadoFactura.EnProceso || x.Estado == EstadoFactura.Autorizada))
                    .SelectMany(x => x.Detalles).Where(x => x.DetalleSustentoId == d.Id).SumAsync(x => (decimal?)x.Cantidad, ct) ?? 0;
                if (p.Cantidad + acreditada > d.Cantidad) throw BusinessException.Conflict("La cantidad supera el saldo disponible para acreditar.");
                var descuento = Math.Round(d.Descuento * p.Cantidad / d.Cantidad, 2, MidpointRounding.AwayFromZero);
                var subtotal = Math.Round(d.PrecioUnitario * p.Cantidad, 2, MidpointRounding.AwayFromZero) - descuento;
                f.Detalles.Add(new() { DetalleSustentoId = d.Id, Codigo = d.Codigo, Descripcion = d.Descripcion, Cantidad = p.Cantidad, PrecioUnitario = d.PrecioUnitario,
                    Descuento = descuento, Subtotal = subtotal, Tarifa = d.Tarifa, Iva = Math.Round(subtotal * FacturaXml.Porcentaje(d.Tarifa) / 100, 2, MidpointRounding.AwayFromZero) });
            }
            f.Subtotal = f.Detalles.Sum(d => d.Subtotal); f.Descuento = f.Detalles.Sum(d => d.Descuento); f.Iva = f.Detalles.Sum(d => d.Iva); f.Total = f.Subtotal + f.Iva;
            var anteriores = await db.FacturasEmitidas.Where(x => x.UsuarioId == usuarioId && x.DocumentoSustentoId == origen.Id && x.TipoDocumento == "04"
                && (x.Estado == EstadoFactura.Firmada || x.Estado == EstadoFactura.Recibida || x.Estado == EstadoFactura.EnProceso || x.Estado == EstadoFactura.Autorizada)).SumAsync(x => (decimal?)x.Total, ct) ?? 0;
            if (anteriores + f.Total > origen.Total) throw BusinessException.Conflict("El importe acreditado supera el saldo de la factura. Revisa descuentos y redondeos del documento original.");
            await ReservarYFirmarAsync(f, new ConstructorNotaCredito(), origen.Ambiente, ct);
            await tx.ConfirmarAsync(ct);
        }
        await facturacion.EnviarYAutorizarAsync(f, ct); return FacturaEmitidaDto.From(f, true);
    }
    public async Task<FacturaEmitidaDto> RetencionAsync(int usuarioId, EmitirRetencionRequest request, CancellationToken ct)
    {
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
        FacturaEmitida f;
        await using (var tx = await control.AbrirAsync(usuarioId, [fecha], ct))
        {
            var c = await db.Comprobantes.Include(c => c.Xml).SingleOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Id == request.ComprobanteId && c.Direccion == Direccion.Recibido && c.TipoComprobante == "01", ct)
                ?? throw BusinessException.NotFound("Factura de compra no encontrada.");
            var usuario = await db.Usuarios.SingleAsync(u => u.Id == usuarioId, ct);
            if (!usuario.Identificaciones().Contains(c.IdentificacionComprador)) throw new BusinessException("El sustento debe estar a nombre del contribuyente.");
            if (request.Detalles.Any(d => d.CodigoImpuesto == "6")) throw new BusinessException("La emisión actual admite retenciones locales de renta e IVA; ISD requiere otro sustento.");
            if (request.Detalles.GroupBy(d => new { d.CodigoImpuesto, d.CodigoRetencion }).Any(g => g.Count() > 1)) throw new BusinessException("No repitas un código de retención.");
            if (request.Detalles.Where(d => d.CodigoImpuesto == "1").Sum(d => d.BaseImponible) > c.Subtotal || request.Detalles.Where(d => d.CodigoImpuesto == "2").Sum(d => d.BaseImponible) > c.Iva)
                throw new BusinessException("Las bases retenidas superan el subtotal o IVA del documento.");
            if (await db.FacturasEmitidas.AnyAsync(x => x.UsuarioId == usuarioId && x.ComprobanteSustentoId == c.Id && x.TipoDocumento == "07"
                && (x.Estado == EstadoFactura.Firmada || x.Estado == EstadoFactura.Recibida || x.Estado == EstadoFactura.EnProceso || x.Estado == EstadoFactura.Autorizada), ct)) throw BusinessException.Conflict("El sustento ya tiene una retención emitida o pendiente.");
            f = new() { UsuarioId = usuarioId, TipoDocumento = "07", FechaEmision = fecha, ComprobanteSustentoId = c.Id,
                NumeroDocumentoSustento = c.NumeroComprobante, FechaDocumentoSustento = c.FechaEmision, CodigoSustento = request.CodigoSustento,
                TipoIdentificacionComprador = "04", IdentificacionComprador = c.RucEmisor, RazonSocialComprador = c.RazonSocialEmisor, FormaPago = "20",
                Retenciones = request.Detalles.Select(d => new RetencionEmitidaDetalle { CodigoImpuesto = d.CodigoImpuesto, CodigoRetencion = d.CodigoRetencion, BaseImponible = d.BaseImponible, Porcentaje = d.Porcentaje,
                    Valor = Math.Round(d.BaseImponible * d.Porcentaje / 100, 2, MidpointRounding.AwayFromZero) }).ToList() };
            f.Subtotal = f.Retenciones.Sum(d => d.BaseImponible); f.Total = f.Retenciones.Sum(d => d.Valor);
            await ReservarYFirmarAsync(f, new ConstructorRetencion(), ClaveAcceso.Ambiente(c.ClaveAcceso) == "2" ? AmbienteSri.Produccion : AmbienteSri.Pruebas, ct, c);
            await tx.ConfirmarAsync(ct);
        }
        await facturacion.EnviarYAutorizarAsync(f, ct); return FacturaEmitidaDto.From(f, true);
    }
    private async Task ReservarYFirmarAsync(FacturaEmitida f, ConstructorDocumentoSri constructor, AmbienteSri ambiente, CancellationToken ct, Comprobante? sustento = null)
    {
        var configuracion = await facturacion.ObtenerDatosAsync(f.UsuarioId);
        if (configuracion.Pendientes.Count > 0) throw new BusinessException("Completa los datos y firma del emisor antes de emitir.");
        var datos = (await db.DatosEmisores.FromSqlInterpolated($"SELECT * FROM datos_emisor WHERE usuario_id = {f.UsuarioId} FOR UPDATE").ToListAsync(ct)).Single();
        if (datos.Ambiente != ambiente) throw BusinessException.Conflict("El ambiente de emisión debe coincidir con el documento de sustento.");
        if (f.TipoDocumento == "07" && !datos.AgenteRetencion) throw BusinessException.Conflict("El titular debe confirmar en sus datos de emisión que está habilitado como agente de retención.");
        var usuario = await db.Usuarios.SingleAsync(u => u.Id == f.UsuarioId, ct);
        if (f.TipoDocumento == "04" && await db.FacturasEmitidas.Where(x => x.Id == f.DocumentoSustentoId).Select(x => x.ClaveAcceso.Substring(10, 13)).SingleAsync(ct) != usuario.Ruc)
            throw BusinessException.Conflict("El RUC actual no coincide con el emisor de la factura original.");
        f.Ambiente = ambiente; f.Establecimiento = datos.Establecimiento; f.PuntoEmision = datos.PuntoEmision; f.Secuencial = datos.SiguienteSecuencial++;
        f.ClaveAcceso = FacturaXml.GenerarClave(f.FechaEmision, usuario.Ruc!, ambiente, f.Establecimiento, f.PuntoEmision, f.Secuencial, tipoDocumento: f.TipoDocumento);
        var firma = await db.FirmasElectronicas.SingleAsync(x => x.UsuarioId == f.UsuarioId, ct);
        var protector = proteccion.CreateProtector("Nudo.FirmaElectronica.v1");
        using var certificado = CertificadoFirma.Abrir(protector.Unprotect(firma.ArchivoCifrado), Encoding.UTF8.GetString(protector.Unprotect(firma.ClaveCifrada)));
        f.XmlFirmado = FirmadorXades.Firmar(NormativaDocumentoSri.AgregarProveedor(constructor.Construir(f, usuario, datos, sustento), f.Ambiente, config), certificado);
        db.FacturasEmitidas.Add(f); control.Auditar(f.UsuarioId, "EmitirComplementario", "DocumentoSri", f.ClaveAcceso, $"Tipo={f.TipoDocumento}; sustento={f.NumeroDocumentoSustento}");
        await db.SaveChangesAsync(ct);
    }
}
