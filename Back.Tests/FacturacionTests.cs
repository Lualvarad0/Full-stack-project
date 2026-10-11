using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Back.Dtos;
using Back.Facturacion;
using Back.Models;
using Back.Sri;
using Xunit;

namespace Back.Tests
{
    public partial class ImportacionSriTests
    {
        private const string ClaveFirma = "clave-de-prueba-123";

        /// <summary>Certificado autofirmado con la estructura de una firma ecuatoriana (titular + SERIALNUMBER).</summary>
        private static X509Certificate2 CertificadoDePrueba(string identificacion = SriFixtures.MiRuc)
        {
            using var rsa = RSA.Create(2048);
            var solicitud = new CertificateRequest($"CN=DAVID ALVARADO, SERIALNUMBER={identificacion}, C=EC", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            solicitud.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, true));
            using var certificado = solicitud.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(2));
            return new X509Certificate2(certificado.Export(X509ContentType.Pkcs12, ClaveFirma), ClaveFirma, X509KeyStorageFlags.Exportable);
        }

        private static MultipartFormDataContent FormularioFirma(X509Certificate2 certificado, string clave)
        {
            var archivo = new ByteArrayContent(certificado.Export(X509ContentType.Pkcs12, ClaveFirma));
            return new MultipartFormDataContent { { archivo, "archivo", "firma.p12" }, { new StringContent(clave), "clave" } };
        }

        private static object FacturaACliente(params object[] detalles) => new
        {
            tipoIdentificacion = "05",
            identificacion = "1710034065",
            razonSocial = "María Alvarado",
            correo = "maria@test.ec",
            direccion = "Quito",
            formaPago = "20",
            detalles,
        };

        [Fact]
        public void Firma_XadesBesVerificableYDetectaAlteraciones()
        {
            using var certificado = CertificadoDePrueba();
            var clave = FacturaXml.GenerarClave(new DateOnly(2026, 10, 11), SriFixtures.MiRuc, AmbienteSri.Pruebas, "001", "001", 7, codigoNumerico: 12345678);
            Assert.True(ClaveAcceso.EsValida(clave));
            Assert.Equal("1", ClaveAcceso.Ambiente(clave));
            Assert.Equal(SriFixtures.MiRuc, ClaveAcceso.RucEmisor(clave));

            var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><factura id=\"comprobante\" version=\"1.1.0\"><infoTributaria><claveAcceso>{clave}</claveAcceso></infoTributaria><infoFactura><importeTotal>11.50</importeTotal></infoFactura></factura>";
            var firmado = FirmadorXades.Firmar(xml, certificado);

            Assert.Contains("<ds:Signature", firmado);
            Assert.Contains("etsi:SignedProperties", firmado);
            Assert.True(FirmadorXades.Verificar(firmado));

            // Verificación independiente con la implementación XMLDSig de .NET: resuelve las tres referencias
            // (SignedProperties, KeyInfo y el comprobante) dentro del documento y valida la firma RSA-SHA1
            // (sin validar la cadena del certificado, que aquí es autofirmado).
            var documento = new System.Xml.XmlDocument { PreserveWhitespace = true };
            documento.LoadXml(firmado);
            var firmaXml = new System.Security.Cryptography.Xml.SignedXml(documento);
            firmaXml.LoadXml((System.Xml.XmlElement)documento.GetElementsByTagName("Signature", FirmadorXades.Ds)[0]!);
            Assert.True(firmaXml.CheckSignature(certificado, verifySignatureOnly: true));

            Assert.False(FirmadorXades.Verificar(firmado.Replace("<importeTotal>11.50", "<importeTotal>1.50")));
        }

        [Fact]
        public async Task Facturacion_PideFirmaYDatosAntesDeEmitir()
        {
            var http = await ClienteAutenticadoAsync("sinfirma@test.ec");

            Assert.Equal(HttpStatusCode.NoContent, (await http.GetAsync("/api/facturacion/firma")).StatusCode);
            var datos = await LeerAsync<DatosEmisorDto>(await http.GetAsync("/api/facturacion/datos"));
            Assert.Contains("tu firma electrónica", datos.Pendientes);
            Assert.Contains("tu RUC en el perfil", datos.Pendientes);

            var emitir = await http.PostAsJsonAsync("/api/facturacion/facturas",
                FacturaACliente(new { descripcion = "Asesoría", cantidad = 1, precioUnitario = 10, descuento = 0, tarifa = "Iva15" }));
            Assert.Equal(HttpStatusCode.BadRequest, emitir.StatusCode);
            Assert.Contains("firma electrónica", await emitir.Content.ReadAsStringAsync());

            using var certificado = CertificadoDePrueba();
            var claveMala = await http.PostAsync("/api/facturacion/firma", FormularioFirma(certificado, "otra-clave"));
            Assert.Equal(HttpStatusCode.BadRequest, claveMala.StatusCode);
            Assert.Contains("contraseña", await claveMala.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Facturacion_EmiteFirmaYAutorizaConElSri()
        {
            var http = await ClienteAutenticadoAsync("emisor@test.ec");
            await ActivarPlanAsync("emisor@test.ec");
            await LeerAsync<PerfilActualizadoDto>(await http.PutAsJsonAsync("/api/perfil",
                new { ruc = SriFixtures.MiRuc, razonSocial = "David Alvarado", regimen = "RimpeEmprendedor", cargasFamiliares = 0 }));

            using var certificado = CertificadoDePrueba();
            var firma = await LeerAsync<FirmaElectronicaDto>(await http.PostAsync("/api/facturacion/firma", FormularioFirma(certificado, ClaveFirma)));
            Assert.Equal("DAVID ALVARADO", firma.Titular);
            Assert.Equal(SriFixtures.MiRuc, firma.Identificacion);
            Assert.True(firma.Vigente);
            Assert.True(firma.CoincideConPerfil);

            var datos = await LeerAsync<DatosEmisorDto>(await http.PutAsJsonAsync("/api/facturacion/datos", new
            {
                ambiente = "Pruebas", establecimiento = "001", puntoEmision = "002", siguienteSecuencial = 15,
                direccionMatriz = "Av. 9 de Octubre 100, Guayaquil", obligadoContabilidad = false,
            }));
            Assert.Empty(datos.Pendientes);

            var factura = await LeerAsync<FacturaEmitidaDto>(await http.PostAsJsonAsync("/api/facturacion/facturas", FacturaACliente(
                new { codigo = "SRV-1", descripcion = "Desarrollo de software", cantidad = 2, precioUnitario = 150, descuento = 20, tarifa = "Iva15" },
                new { descripcion = "Libro técnico", cantidad = 1, precioUnitario = 25.5, descuento = 0, tarifa = "Iva0" })));

            Assert.Equal(EstadoFactura.Autorizada, factura.Estado);
            Assert.Equal("001-002-000000015", factura.Numero);
            Assert.Equal(305.50m, factura.Subtotal);
            Assert.Equal(42.00m, factura.Iva);
            Assert.Equal(347.50m, factura.Total);
            Assert.Equal("MARÍA ALVARADO", factura.RazonSocialComprador);
            Assert.Equal(factura.ClaveAcceso, factura.NumeroAutorizacion);
            Assert.True(ClaveAcceso.EsValida(factura.ClaveAcceso));

            // El XML que llegó al SRI está firmado correctamente y trae los datos de la ficha técnica.
            var enviado = _factory.Sri.Recibidos[factura.ClaveAcceso];
            Assert.True(FirmadorXades.Verificar(enviado));
            Assert.Contains("<ambiente>1</ambiente>", enviado);
            Assert.Contains("<secuencial>000000015</secuencial>", enviado);
            Assert.Contains("<contribuyenteRimpe>CONTRIBUYENTE RÉGIMEN RIMPE</contribuyenteRimpe>", enviado);
            Assert.Contains("<codigoPorcentaje>4</codigoPorcentaje><baseImponible>280.00</baseImponible><valor>42.00</valor>", enviado);
            Assert.Contains("<importeTotal>347.50</importeTotal>", enviado);

            // El secuencial avanza; en ambiente de pruebas la factura no se suma a los ingresos.
            var segunda = await LeerAsync<FacturaEmitidaDto>(await http.PostAsJsonAsync("/api/facturacion/facturas",
                FacturaACliente(new { descripcion = "Soporte", cantidad = 1, precioUnitario = 40, descuento = 0, tarifa = "Iva15" })));
            Assert.Equal("001-002-000000016", segunda.Numero);
            Assert.Empty(await LeerAsync<List<ComprobanteListaDto>>(await http.GetAsync("/api/comprobantes?direccion=Emitido")));

            var lista = await LeerAsync<List<FacturaEmitidaDto>>(await http.GetAsync("/api/facturacion/facturas"));
            Assert.Equal(2, lista.Count);
            var detalle = await LeerAsync<FacturaEmitidaDto>(await http.GetAsync($"/api/facturacion/facturas/{factura.Id}"));
            Assert.Equal(2, detalle.Detalles!.Count);

            // A consumidor final solo hasta $50.
            var consumidorFinal = await http.PostAsJsonAsync("/api/facturacion/facturas", new
            {
                tipoIdentificacion = "07", identificacion = "", razonSocial = "x", formaPago = "01",
                detalles = new[] { new { descripcion = "Varios", cantidad = 1, precioUnitario = 60, descuento = 0, tarifa = "Iva15" } },
            });
            Assert.Equal(HttpStatusCode.BadRequest, consumidorFinal.StatusCode);

            // Una autorizada no se borra: se anula.
            Assert.Equal(HttpStatusCode.Conflict, (await http.DeleteAsync($"/api/facturacion/facturas/{factura.Id}")).StatusCode);
            var anulada = await LeerAsync<FacturaEmitidaDto>(await http.PostAsync($"/api/facturacion/facturas/{factura.Id}/anular", null));
            Assert.Equal(EstadoFactura.Anulada, anulada.Estado);
            Assert.Contains("SRI en Línea", anulada.Mensajes);

            // Catálogo de servicios para facturar rápido.
            var servicio = await LeerAsync<ServicioFacturableDto>(await http.PostAsJsonAsync("/api/facturacion/servicios",
                new { codigo = "ASE-1", descripcion = "Asesoría tributaria", precioUnitario = 45.5, tarifa = "Iva15" }));
            var editado = await LeerAsync<ServicioFacturableDto>(await http.PutAsJsonAsync($"/api/facturacion/servicios/{servicio.Id}",
                new { codigo = "ASE-1", descripcion = "Asesoría tributaria mensual", precioUnitario = 50, tarifa = "Iva15" }));
            Assert.Equal(50m, editado.PrecioUnitario);
            Assert.Single(await LeerAsync<List<ServicioFacturableDto>>(await http.GetAsync("/api/facturacion/servicios")));
            Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"/api/facturacion/servicios/{servicio.Id}")).StatusCode);

            // Subir la firma y cambiar los datos de facturación quedan en la actividad de la cuenta.
            var avisos = await LeerAsync<List<AvisoCuentaDto>>(await http.GetAsync("/api/avisos"));
            Assert.Contains(avisos, a => a.Tipo == TipoAviso.FirmaElectronica);
            Assert.Contains(avisos, a => a.Tipo == TipoAviso.DatosFacturacion);
        }

        [Fact]
        public async Task Facturacion_GuardaLosMotivosCuandoElSriLaDevuelve()
        {
            var http = await ClienteAutenticadoAsync("devuelta@test.ec");
            await ActivarPlanAsync("devuelta@test.ec");
            await LeerAsync<PerfilActualizadoDto>(await http.PutAsJsonAsync("/api/perfil",
                new { ruc = SriFixtures.MiRuc, razonSocial = "David Alvarado", regimen = "General", cargasFamiliares = 0 }));
            using var certificado = CertificadoDePrueba();
            await LeerAsync<FirmaElectronicaDto>(await http.PostAsync("/api/facturacion/firma", FormularioFirma(certificado, ClaveFirma)));
            await LeerAsync<DatosEmisorDto>(await http.PutAsJsonAsync("/api/facturacion/datos", new
            {
                ambiente = "Pruebas", establecimiento = "001", puntoEmision = "001", siguienteSecuencial = 1,
                direccionMatriz = "Quito", obligadoContabilidad = false,
            }));

            _factory.Sri.RecepcionDevuelta = SriFixtures.RespuestaDevuelta("35", "ARCHIVO NO CUMPLE ESTRUCTURA XML");
            try
            {
                var factura = await LeerAsync<FacturaEmitidaDto>(await http.PostAsJsonAsync("/api/facturacion/facturas",
                    FacturaACliente(new { descripcion = "Consultoría", cantidad = 1, precioUnitario = 100, descuento = 0, tarifa = "Iva15" })));
                Assert.Equal(EstadoFactura.Devuelta, factura.Estado);
                Assert.Contains("ARCHIVO NO CUMPLE ESTRUCTURA XML", factura.Mensajes);

                var reintento = await http.PostAsync($"/api/facturacion/facturas/{factura.Id}/reintentar", null);
                Assert.Equal(HttpStatusCode.BadRequest, reintento.StatusCode);

                // Una factura devuelta nunca existió para el SRI: se puede borrar.
                Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"/api/facturacion/facturas/{factura.Id}")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"/api/facturacion/facturas/{factura.Id}")).StatusCode);
            }
            finally
            {
                _factory.Sri.RecepcionDevuelta = null;
            }
        }
    }
}
