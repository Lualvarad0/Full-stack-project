using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Back.Controllers;
using Back.Dtos;
using Back.Models;
using MimeKit;

namespace Back.Tests
{
    public partial class ImportacionSriTests
    {
        [Fact]
        public async Task Filtros_PorRangoDeFechasYMontos()
        {
            var http = await ClienteAutenticadoAsync("filtros@test.ec");
            var enero = new DateOnly(2026, 1, 20);
            var marzo = new DateOnly(2026, 3, 5);
            var abril = new DateOnly(2026, 4, 2);
            var fEnero = Autorizar(SriFixtures.Clave(enero, "01", SriFixtures.RucFarmacia, 901),
                SriFixtures.FacturaXml(SriFixtures.Clave(enero, "01", SriFixtures.RucFarmacia, 901), SriFixtures.RucFarmacia, "FARMACIA", enero));
            var ncMarzo = Autorizar(SriFixtures.Clave(marzo, "04", SriFixtures.RucFarmacia, 902),
                SriFixtures.NotaCreditoXml(SriFixtures.Clave(marzo, "04", SriFixtures.RucFarmacia, 902), SriFixtures.RucFarmacia, marzo));
            var fAbril = Autorizar(SriFixtures.Clave(abril, "01", SriFixtures.RucFarmacia, 903),
                SriFixtures.FacturaXml(SriFixtures.Clave(abril, "01", SriFixtures.RucFarmacia, 903), SriFixtures.RucFarmacia, "FARMACIA", abril));
            await LeerAsync<ImportacionResponse>(await http.PostAsJsonAsync("/api/comprobantes/importar/claves", new { texto = $"{fEnero} {ncMarzo} {fAbril}" }));

            async Task<string[]> Claves(string query) =>
                (await LeerAsync<List<ComprobanteListaDto>>(await http.GetAsync($"/api/comprobantes?{query}"))).Select(c => c.ClaveAcceso).ToArray();

            // Rango de fechas inclusivo que cruza meses.
            Assert.Equal(new[] { ncMarzo, fEnero }, await Claves("desde=2026-01-20&hasta=2026-03-05"));
            // Montos: la factura vale 33,00 y la nota de crédito 11,50.
            Assert.Equal(new[] { fAbril, fEnero }, await Claves("min=20"));
            Assert.Equal(new[] { ncMarzo }, await Claves("max=20"));
            Assert.Equal(new[] { fAbril }, await Claves("desde=2026-03-06&min=30&max=40"));

            Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/comprobantes?desde=2026-05-01&hasta=2026-04-01")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/comprobantes?min=50&max=10")).StatusCode);
        }

        private HttpRequestMessage Webhook(HttpContent contenido, string? para, string secreto = ApiFactory.SecretoWebhook)
        {
            var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/correo/entrante" + (para is null ? "" : $"?para={Uri.EscapeDataString(para)}")) { Content = contenido };
            peticion.Headers.Add(CorreoController.EncabezadoSecreto, secreto);
            return peticion;
        }

        private static StreamContent Mime(MimeMessage mensaje)
        {
            var memoria = new MemoryStream();
            mensaje.WriteTo(memoria);
            memoria.Position = 0;
            var contenido = new StreamContent(memoria);
            contenido.Headers.ContentType = new MediaTypeHeaderValue("message/rfc822");
            return contenido;
        }

        private static byte[] Zip(string nombre, string xml)
        {
            using var memoria = new MemoryStream();
            using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
            using (var escritor = new StreamWriter(zip.CreateEntry(nombre).Open()))
                escritor.Write(xml);
            return memoria.ToArray();
        }

        /// <summary>Espera a que el procesador en segundo plano termine con el último correo del buzón.</summary>
        private static async Task<BuzonDto> EsperarProcesadoAsync(HttpClient http)
        {
            for (var i = 0; i < 50; i++)
            {
                var buzon = await LeerAsync<BuzonDto>(await http.GetAsync("/api/correo"));
                if (buzon.Recientes.Count > 0 && buzon.Recientes[0].Estado != EstadoCorreo.Pendiente) return buzon;
                await Task.Delay(200);
            }
            throw new TimeoutException("El correo no se procesó a tiempo.");
        }

        [Fact]
        public async Task Buzon_CorreoReenviadoConXmlYZipSeImportaComoGastoEIngreso()
        {
            var http = await ClienteAutenticadoAsync("buzon@test.ec");
            await LeerAsync<PerfilActualizadoDto>(await http.PutAsJsonAsync("/api/perfil", new { ruc = SriFixtures.MiRuc, regimen = "General", cargasFamiliares = 0 }));

            var buzon = await LeerAsync<BuzonDto>(await http.GetAsync("/api/correo"));
            Assert.True(buzon.Configurado);
            Assert.Equal($"{buzon.Codigo}@{ApiFactory.DominioCorreo}", buzon.Direccion);
            Assert.Equal(buzon.Codigo, (await LeerAsync<BuzonDto>(await http.GetAsync("/api/correo"))).Codigo); // estable

            var fecha = new DateOnly(2026, 10, 20);
            var gasto = Autorizar(SriFixtures.Clave(fecha, "01", SriFixtures.RucSupermercado, 811),
                SriFixtures.FacturaXml(SriFixtures.Clave(fecha, "01", SriFixtures.RucSupermercado, 811), SriFixtures.RucSupermercado, "SUPERMERCADO", fecha, comprador: SriFixtures.MiCedula));
            var ingreso = Autorizar(SriFixtures.Clave(fecha, "01", SriFixtures.MiRuc, 812),
                SriFixtures.FacturaXml(SriFixtures.Clave(fecha, "01", SriFixtures.MiRuc, 812), SriFixtures.MiRuc, "DAVID ALVARADO", fecha,
                    comprador: "1790010937001", razonComprador: "CLIENTE DEL BUZON S.A."));

            // Correo reenviado por Gmail: el To original es el Gmail del usuario; la entrega real va en ?para=.
            var cuerpo = new BodyBuilder { TextBody = "Adjuntamos su factura electrónica." };
            cuerpo.Attachments.Add("FAC-811.xml", Encoding.UTF8.GetBytes($"<factura><infoTributaria><claveAcceso>{gasto}</claveAcceso></infoTributaria></factura>"), new ContentType("application", "xml"));
            cuerpo.Attachments.Add("emitidas.zip", Zip("FAC-812.xml", $"<factura><infoTributaria><claveAcceso>{ingreso}</claveAcceso></infoTributaria></factura>"), new ContentType("application", "zip"));
            cuerpo.Attachments.Add("RIDE.pdf", Encoding.UTF8.GetBytes("%PDF-1.4"), new ContentType("application", "pdf"));
            var mensaje = new MimeMessage { Subject = "Su factura electrónica", Body = cuerpo.ToMessageBody() };
            mensaje.From.Add(MailboxAddress.Parse("facturacion@supermercado.ec"));
            mensaje.To.Add(MailboxAddress.Parse("david@gmail.com"));

            var anonimo = _factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.SendAsync(Webhook(Mime(mensaje), buzon.Direccion, "secreto-incorrecto"))).StatusCode);
            Assert.Equal(HttpStatusCode.Accepted, (await anonimo.SendAsync(Webhook(Mime(mensaje), buzon.Direccion))).StatusCode);

            var procesado = await EsperarProcesadoAsync(http);
            var correo = procesado.Recientes[0];
            Assert.Equal(EstadoCorreo.Procesado, correo.Estado);
            Assert.Equal("facturacion@supermercado.ec", correo.Remitente);
            Assert.Equal(2, correo.ArchivosXml);
            Assert.Equal(2, correo.Importados);
            Assert.Equal(1, correo.Ingresos);

            var ingresos = await LeerAsync<List<ComprobanteListaDto>>(await http.GetAsync("/api/comprobantes?direccion=Emitido&anio=2026&mes=10"));
            Assert.Equal("CLIENTE DEL BUZON S.A.", Assert.Single(ingresos).RazonSocialComprador);
            Assert.Equal(gasto, Assert.Single(await LeerAsync<List<ComprobanteListaDto>>(await http.GetAsync("/api/comprobantes?anio=2026&mes=10"))).ClaveAcceso);

            // Un correo a una dirección que no existe se acepta y se descarta (el proveedor no reintenta).
            Assert.Equal(HttpStatusCode.Accepted, (await anonimo.SendAsync(Webhook(Mime(mensaje), $"zzzzzzzzzz@{ApiFactory.DominioCorreo}"))).StatusCode);
            Assert.Single((await LeerAsync<BuzonDto>(await http.GetAsync("/api/correo"))).Recientes);
        }

        [Fact]
        public async Task Buzon_ConfirmacionDeGmailYFormularioMultipartYRegenerar()
        {
            var http = await ClienteAutenticadoAsync("buzon2@test.ec");
            var buzon = await LeerAsync<BuzonDto>(await http.GetAsync("/api/correo"));
            var anonimo = _factory.CreateClient();

            // 1. Gmail pide confirmar la dirección de reenvío: Nudo guarda el código y el enlace.
            var confirmacion = new MimeMessage
            {
                Subject = "(#123456789) Confirmación de reenvío de Gmail - Recibir correo de david@gmail.com",
                Body = new TextPart("plain") { Text = "Para permitir el reenvío haz clic en el enlace:\nhttps://mail-settings.google.com/mail/vf-%5BABC%5D-xyz\n\nCódigo de confirmación: 123456789" },
            };
            confirmacion.From.Add(MailboxAddress.Parse("forwarding-noreply@google.com"));
            confirmacion.To.Add(MailboxAddress.Parse(buzon.Direccion!)); // sin ?para=: se toma del encabezado To
            Assert.Equal(HttpStatusCode.Accepted, (await anonimo.SendAsync(Webhook(Mime(confirmacion), null))).StatusCode);

            var conCodigo = await LeerAsync<BuzonDto>(await http.GetAsync("/api/correo"));
            Assert.Equal("123456789", conCodigo.ConfirmacionGmailCodigo);
            Assert.Equal("https://mail-settings.google.com/mail/vf-%5BABC%5D-xyz", conCodigo.ConfirmacionGmailEnlace);
            Assert.Equal(EstadoCorreo.ConfirmacionGmail, conCodigo.Recientes[0].Estado);

            // 2. Proveedor tipo Mailgun/SendGrid: formulario multipart con el destinatario y los adjuntos.
            var fecha = new DateOnly(2026, 10, 21);
            var clave = Autorizar(SriFixtures.Clave(fecha, "01", SriFixtures.RucFarmacia, 821),
                SriFixtures.FacturaXml(SriFixtures.Clave(fecha, "01", SriFixtures.RucFarmacia, 821), SriFixtures.RucFarmacia, "FARMACIA", fecha));
            var formulario = new MultipartFormDataContent
            {
                { new StringContent(buzon.Direccion!), "recipient" },
                { new StringContent("Farmacia <facturas@farmacia.ec>"), "sender" },
                { new StringContent("Factura 821"), "subject" },
                { new ByteArrayContent(Encoding.UTF8.GetBytes($"<factura><infoTributaria><claveAcceso>{clave}</claveAcceso></infoTributaria></factura>")), "attachment-1", "FAC-821.xml" },
            };
            Assert.Equal(HttpStatusCode.Accepted, (await anonimo.SendAsync(Webhook(formulario, null))).StatusCode);
            Assert.Equal(1, (await EsperarProcesadoAsync(http)).Recientes[0].Importados);

            // 3. Un correo sin comprobantes queda registrado como tal.
            var publicidad = new MimeMessage { Subject = "Ofertas", Body = new TextPart("plain") { Text = "Promociones del mes" } };
            publicidad.From.Add(MailboxAddress.Parse("ofertas@tienda.ec"));
            await anonimo.SendAsync(Webhook(Mime(publicidad), buzon.Direccion));
            Assert.Equal(EstadoCorreo.SinComprobantes, (await LeerAsync<BuzonDto>(await http.GetAsync("/api/correo"))).Recientes[0].Estado);

            // 4. Regenerar: cambia la dirección y la anterior deja de recibir.
            var nuevo = await LeerAsync<BuzonDto>(await http.PostAsync("/api/correo/regenerar", null));
            Assert.NotEqual(buzon.Codigo, nuevo.Codigo);
            Assert.Null(nuevo.ConfirmacionGmailCodigo);
            await anonimo.SendAsync(Webhook(Mime(publicidad), buzon.Direccion));
            Assert.Equal(3, (await LeerAsync<BuzonDto>(await http.GetAsync("/api/correo"))).Recientes.Count);

            var avisos = await LeerAsync<List<AvisoCuentaDto>>(await http.GetAsync("/api/avisos"));
            Assert.Contains(avisos, a => a.Tipo == TipoAviso.BuzonRegenerado);
        }

        [Fact]
        public async Task Avisos_IngresoYCambiosDeConfiguracionQuedanRegistrados()
        {
            var http = await ClienteAutenticadoAsync("avisos@test.ec");
            var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "avisos@test.ec", password = "Secreta123" });
            login.EnsureSuccessStatusCode();
            await LeerAsync<PerfilActualizadoDto>(await http.PutAsJsonAsync("/api/perfil", new { ruc = SriFixtures.MiRuc, regimen = "General", cargasFamiliares = 1 }));
            // Guardar sin cambios no genera aviso.
            await LeerAsync<PerfilActualizadoDto>(await http.PutAsJsonAsync("/api/perfil", new { ruc = SriFixtures.MiRuc, regimen = "General", cargasFamiliares = 1 }));

            var avisos = await LeerAsync<List<AvisoCuentaDto>>(await http.GetAsync("/api/avisos"));
            Assert.Equal(new[] { TipoAviso.PerfilActualizado, TipoAviso.Ingreso, TipoAviso.Registro }, avisos.Select(a => a.Tipo).ToArray());
            Assert.Equal("Cambios: RUC, cédula, régimen, cargas familiares.", avisos[0].Detalle);
        }
    }
}
