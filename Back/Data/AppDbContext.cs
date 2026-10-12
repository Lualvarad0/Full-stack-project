using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Factura> Facturas => Set<Factura>();
        public DbSet<DetalleFactura> DetallesFactura => Set<DetalleFactura>();
        public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
        public DbSet<ComprobanteXml> ComprobantesXml => Set<ComprobanteXml>();
        public DbSet<ProveedorCategoria> ProveedoresCategoria => Set<ProveedorCategoria>();
        public DbSet<BuzonCorreo> BuzonesCorreo => Set<BuzonCorreo>();
        public DbSet<CorreoRecibido> CorreosRecibidos => Set<CorreoRecibido>();
        public DbSet<AvisoCuenta> AvisosCuenta => Set<AvisoCuenta>();
        public DbSet<FirmaElectronica> FirmasElectronicas => Set<FirmaElectronica>();
        public DbSet<DatosEmisor> DatosEmisores => Set<DatosEmisor>();
        public DbSet<FacturaEmitida> FacturasEmitidas => Set<FacturaEmitida>();
        public DbSet<Suscripcion> Suscripciones => Set<Suscripcion>();
        public DbSet<ServicioFacturable> ServiciosFacturables => Set<ServicioFacturable>();
        public DbSet<PagoSuscripcion> PagosSuscripcion => Set<PagoSuscripcion>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CargaFamiliar>(e => {
                e.ToTable("cargas_familiares"); e.Property(x => x.Identificacion).HasMaxLength(20); e.Property(x => x.Nombre).HasMaxLength(200);
                e.Property(x => x.Parentesco).HasConversion<string>().HasMaxLength(30); e.Property(x => x.IngresosGravados).HasPrecision(18, 2);
                e.HasIndex(x => new { x.UsuarioId, x.Anio, x.Identificacion }).IsUnique();
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PerfilObligaciones>(e => {
                e.ToTable("perfil_obligaciones"); e.HasKey(x => x.UsuarioId); e.Property(x => x.Actividades).HasMaxLength(2000);
                foreach (var p in new[] { "Iva", "Retenciones", "Ats" }) e.Property<FrecuenciaObligacion>(p).HasConversion<string>().HasMaxLength(20);
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PreparacionRenta>(e => {
                e.ToTable("preparacion_renta"); e.HasIndex(x => new { x.UsuarioId, x.Anio }).IsUnique();
                foreach (var p in new[] { "SueldosGravados", "IessPersonal", "RetencionesSueldo", "OtrosIngresosGeneral", "OtrosGastosGeneral", "IngresosRimpeAjuste", "Exoneraciones", "AnticiposRenta" }) e.Property<decimal>(p).HasPrecision(18, 2);
                e.Property(x => x.SustentoAjustes).HasMaxLength(2000); e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PreparacionIva>(e => {
                e.ToTable("preparacion_iva"); e.HasIndex(x => new { x.UsuarioId, x.Anio, x.Mes, x.Meses }).IsUnique();
                e.Property(x => x.FactorCredito).HasPrecision(6, 4);
                foreach (var p in new[] { "CreditoAnterior", "AjusteDebito", "AjusteCredito" }) e.Property<decimal>(p).HasPrecision(18, 2);
                e.Property(x => x.SustentoAjustes).HasMaxLength(2000); e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<RevisionFiscalComprobante>(e => {
                e.ToTable("revision_fiscal_comprobantes"); e.HasKey(x => x.ComprobanteId); e.Property(x => x.Renta).HasConversion<string>().HasMaxLength(30);
                e.Property(x => x.CodigoSustento).HasMaxLength(2); e.Property(x => x.FormaPago).HasMaxLength(2); e.Property(x => x.Observacion).HasMaxLength(1000);
                e.HasOne<Comprobante>().WithMany().HasForeignKey(x => x.ComprobanteId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<RevisionTributaria>(e => {
                e.ToTable("revisiones_tributarias"); e.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30); e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(30);
                e.Property(x => x.Huella).HasMaxLength(64); e.Property(x => x.ReferenciaPresentacion).HasMaxLength(150);
                e.HasIndex(x => new { x.UsuarioId, x.Anio, x.Tipo }); e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CuentaContable>(e => {
                e.ToTable("cuentas_contables"); e.Property(x => x.Codigo).HasMaxLength(30); e.Property(x => x.Nombre).HasMaxLength(150);
                e.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20); e.HasIndex(x => new { x.UsuarioId, x.Codigo }).IsUnique();
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AsientoContable>(e => {
                e.ToTable("asientos_contables"); e.Property(x => x.Concepto).HasMaxLength(500); e.Property(x => x.MotivoAnulacion).HasMaxLength(1000);
                e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20); e.HasIndex(x => new { x.UsuarioId, x.Fecha });
                e.HasIndex(x => new { x.UsuarioId, x.ComprobanteId }).IsUnique().HasFilter("\"comprobante_id\" IS NOT NULL AND \"estado\" <> 'Anulado'");
                e.HasIndex(x => new { x.UsuarioId, x.PagoComprobanteId }).IsUnique().HasFilter("\"pago_comprobante_id\" IS NOT NULL AND \"estado\" <> 'Anulado'");
                e.HasOne<Comprobante>().WithMany().HasForeignKey(x => x.ComprobanteId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<PagoComprobante>().WithMany().HasForeignKey(x => x.PagoComprobanteId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(x => x.Lineas).WithOne().HasForeignKey(x => x.AsientoContableId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<LineaAsiento>(e => {
                e.ToTable("lineas_asientos"); e.Property(x => x.Debe).HasPrecision(18, 2); e.Property(x => x.Haber).HasPrecision(18, 2);
                e.HasOne(x => x.Cuenta).WithMany().HasForeignKey(x => x.CuentaContableId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PagoComprobante>(e => {
                e.ToTable("pagos_comprobantes"); e.Property(p => p.Importe).HasPrecision(18, 2); e.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(20); e.Property(p => p.Medio).HasConversion<string>().HasMaxLength(20);
                e.Property(p => p.Referencia).HasMaxLength(100); e.Property(p => p.MotivoAnulacion).HasMaxLength(1000); e.HasIndex(p => new { p.UsuarioId, p.Fecha });
                e.HasOne(p => p.Comprobante).WithMany().HasForeignKey(p => p.ComprobanteId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<CuentaBancaria>().WithMany().HasForeignKey(p => p.CuentaBancariaId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CuentaBancaria>(e => {
                e.ToTable("cuentas_bancarias"); e.Property(x => x.Nombre).HasMaxLength(100); e.Property(x => x.Entidad).HasMaxLength(100); e.Property(x => x.UltimosDigitos).HasMaxLength(4); e.Property(x => x.SaldoInicial).HasPrecision(18, 2);
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<MovimientoBancario>(e => {
                e.ToTable("movimientos_bancarios"); e.Property(x => x.Importe).HasPrecision(18, 2); e.Property(x => x.IdExterno).HasMaxLength(100); e.Property(x => x.Concepto).HasMaxLength(300); e.Property(x => x.Referencia).HasMaxLength(100);
                e.Property(x => x.MotivoRevision).HasMaxLength(1000);
                e.HasIndex(x => new { x.CuentaBancariaId, x.IdExterno }).IsUnique();
                e.HasOne<CuentaBancaria>().WithMany().HasForeignKey(x => x.CuentaBancariaId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ConciliacionBancaria>(e => {
                e.ToTable("conciliaciones_bancarias"); e.Property(x => x.Importe).HasPrecision(18, 2); e.HasIndex(x => new { x.MovimientoBancarioId, x.PagoComprobanteId }).IsUnique();
                e.HasOne<MovimientoBancario>().WithMany().HasForeignKey(x => x.MovimientoBancarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<PagoComprobante>().WithMany().HasForeignKey(x => x.PagoComprobanteId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<RetencionSri>(e => {
                e.ToTable("retenciones_sri"); e.Property(x => x.Direccion).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.ClaveAcceso).HasMaxLength(49); e.Property(x => x.Numero).HasMaxLength(20);
                e.Property(x => x.RucEmisor).HasMaxLength(13); e.Property(x => x.IdentificacionRetenido).HasMaxLength(20);
                e.Property(x => x.RazonSocialEmisor).HasMaxLength(300); e.Property(x => x.RazonSocialRetenido).HasMaxLength(300); e.Property(x => x.NumeroAutorizacion).HasMaxLength(49);
                e.HasIndex(x => new { x.UsuarioId, x.ClaveAcceso }).IsUnique();
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.RetencionSriId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<RetencionSriDetalle>(e => {
                e.ToTable("retenciones_sri_detalle"); e.Property(x => x.CodigoImpuesto).HasMaxLength(2); e.Property(x => x.CodigoRetencion).HasMaxLength(10);
                e.Property(x => x.DocumentoSustento).HasMaxLength(49); e.Property(x => x.BaseImponible).HasPrecision(18, 2);
                e.Property(x => x.Porcentaje).HasPrecision(8, 4); e.Property(x => x.Valor).HasPrecision(18, 2);
                e.HasOne<Comprobante>().WithMany().HasForeignKey(x => x.ComprobanteId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<RetencionEmitidaDetalle>(e => {
                e.ToTable("retenciones_emitidas_detalle"); e.Property(x => x.CodigoImpuesto).HasMaxLength(2); e.Property(x => x.CodigoRetencion).HasMaxLength(10);
                e.Property(x => x.BaseImponible).HasPrecision(18, 2); e.Property(x => x.Porcentaje).HasPrecision(8, 4); e.Property(x => x.Valor).HasPrecision(18, 2);
            });
            modelBuilder.Entity<PeriodoContable>(e => {
                e.ToTable("periodos_contables"); e.HasIndex(x => new { x.UsuarioId, x.Anio, x.Mes }).IsUnique();
                e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AuditoriaContable>(e => {
                e.ToTable("auditoria_contable"); e.Property(x => x.Accion).HasMaxLength(80); e.Property(x => x.Entidad).HasMaxLength(80);
                e.Property(x => x.Referencia).HasMaxLength(100); e.Property(x => x.Detalle).HasMaxLength(2000);
                e.HasIndex(x => new { x.UsuarioId, x.Fecha });
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<VinculoContador>(e => {
                e.ToTable("vinculos_contador");
                e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.Permiso).HasConversion<string>().HasMaxLength(20);
                e.HasIndex(x => new { x.PropietarioId, x.ContadorId }).IsUnique();
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.PropietarioId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.ContadorId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TokenCuenta>(e => {
                e.ToTable("tokens_cuenta");
                e.Property(x => x.Hash).HasMaxLength(64); e.HasIndex(x => x.Hash).IsUnique();
                e.Property(x => x.Proposito).HasConversion<string>().HasMaxLength(30);
                e.HasIndex(x => new { x.UsuarioId, x.Proposito, x.CreadoEn });
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<SesionCuenta>(e => {
                e.ToTable("sesiones_cuenta");
                e.Property(x => x.Id).HasMaxLength(36); e.Property(x => x.Dispositivo).HasMaxLength(100);
                e.HasIndex(x => new { x.UsuarioId, x.ExpiraEn });
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<CorreoAccesoPendiente>(e => {
                e.ToTable("correos_acceso_pendientes");
                e.HasIndex(x => new { x.EnviadoEn, x.ProximoIntentoEn });
                e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Usuario>(e =>
            {
                e.Property(u => u.Nombre).HasMaxLength(100);
                e.Property(u => u.Correo).HasMaxLength(150);
                e.Property(u => u.Rol).HasMaxLength(20);
                e.Property(u => u.Cedula).HasMaxLength(10);
                e.Property(u => u.Ruc).HasMaxLength(13);
                e.Property(u => u.RazonSocial).HasMaxLength(300);
                e.Property(u => u.Regimen).HasConversion<string>().HasMaxLength(30);
                e.Property(u => u.Apellido).HasMaxLength(100);
                e.Property(u => u.Plan).HasConversion<string>().HasMaxLength(20);
                e.HasIndex(u => u.Correo).IsUnique();
            });

            modelBuilder.Entity<Cliente>(e =>
            {
                e.Property(c => c.Identificacion).HasMaxLength(20);
                e.Property(c => c.Nombre).HasMaxLength(150);
                e.Property(c => c.Telefono).HasMaxLength(20);
                e.Property(c => c.Correo).HasMaxLength(150);
                e.HasIndex(c => new { c.UsuarioId, c.Identificacion }).IsUnique();
                e.HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Producto>(e =>
            {
                e.Property(p => p.Nombre).HasMaxLength(150);
                e.HasIndex(p => p.UsuarioId);
                e.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.Property(p => p.Precio).HasPrecision(12, 2);
                e.Property(p => p.Version).IsRowVersion();
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_productos_precio", "precio >= 0");
                    t.HasCheckConstraint("ck_productos_stock", "stock >= 0");
                });
            });

            modelBuilder.Entity<Factura>(e =>
            {
                e.Property(f => f.FormaPago).HasMaxLength(50);
                e.Property(f => f.MontoTotal).HasPrecision(12, 2);
                e.HasOne(f => f.Cliente).WithMany().HasForeignKey(f => f.ClienteId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(f => f.Usuario).WithMany().HasForeignKey(f => f.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(f => f.Detalles).WithOne(d => d.Factura).HasForeignKey(d => d.FacturaId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(f => f.Fecha);
            });

            modelBuilder.Entity<DetalleFactura>(e =>
            {
                e.ToTable("detalles_factura");
                e.Property(d => d.PrecioUnitario).HasPrecision(12, 2);
                e.Property(d => d.PrecioTotal).HasPrecision(12, 2);
                e.HasOne(d => d.Producto).WithMany().HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Comprobante>(e =>
            {
                e.ToTable("comprobantes");
                e.Property(c => c.ClaveAcceso).HasMaxLength(49).IsFixedLength();
                e.Property(c => c.TipoComprobante).HasMaxLength(2).IsFixedLength();
                e.Property(c => c.NumeroComprobante).HasMaxLength(17);
                e.Property(c => c.NumeroAutorizacion).HasMaxLength(49);
                e.Property(c => c.RucEmisor).HasMaxLength(13);
                e.Property(c => c.RazonSocialEmisor).HasMaxLength(300);
                e.Property(c => c.NombreComercial).HasMaxLength(300);
                e.Property(c => c.IdentificacionComprador).HasMaxLength(20);
                e.Property(c => c.RazonSocialComprador).HasMaxLength(300);
                e.Property(c => c.DocumentoModificado).HasMaxLength(17);
                e.Property(c => c.Categoria).HasConversion<string>().HasMaxLength(20);
                e.Property(c => c.CategoriaIngreso).HasConversion<string>().HasMaxLength(20);
                e.Property(c => c.Finalidad).HasConversion<string>().HasMaxLength(20);
                e.Property(c => c.TratamientoIva).HasConversion<string>().HasMaxLength(20);
                e.Property(c => c.Direccion).HasConversion<string>().HasMaxLength(10);
                foreach (var monto in new[] { "Subtotal", "BaseIva0", "BaseIvaGravada", "Iva", "OtrosImpuestos", "Descuento", "Propina", "Total" })
                    e.Property(monto).HasPrecision(12, 2);
                e.Ignore(c => c.Signo);

                e.HasIndex(c => new { c.UsuarioId, c.ClaveAcceso }).IsUnique();
                e.HasIndex(c => new { c.UsuarioId, c.Direccion, c.FechaEmision });
                e.HasIndex(c => new { c.UsuarioId, c.RucEmisor });
                e.HasOne(c => c.Usuario).WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Cascade);
                e.HasMany(c => c.Detalles).WithOne().HasForeignKey(d => d.ComprobanteId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.Xml).WithOne().HasForeignKey<ComprobanteXml>(x => x.ComprobanteId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ComprobanteDetalle>(e =>
            {
                e.ToTable("comprobantes_detalle");
                e.Property(d => d.Codigo).HasMaxLength(50);
                e.Property(d => d.Descripcion).HasMaxLength(500);
                e.Property(d => d.Cantidad).HasPrecision(18, 6);
                e.Property(d => d.PrecioUnitario).HasPrecision(18, 6);
                e.Property(d => d.Descuento).HasPrecision(12, 2);
                e.Property(d => d.Subtotal).HasPrecision(12, 2);
                e.Property(d => d.Categoria).HasConversion<string>().HasMaxLength(20);
                e.Property(d => d.CategoriaIngreso).HasConversion<string>().HasMaxLength(20);
                e.Property(d => d.Finalidad).HasConversion<string>().HasMaxLength(20);
                e.Property(d => d.TratamientoIva).HasConversion<string>().HasMaxLength(20);
                e.Property(d => d.Iva).HasPrecision(12, 2);
                e.Property(d => d.TarifaIva).HasPrecision(6, 2);
            });

            modelBuilder.Entity<ComprobanteXml>(e =>
            {
                e.ToTable("comprobantes_xml");
                e.HasKey(x => x.ComprobanteId);
            });

            modelBuilder.Entity<BuzonCorreo>(e =>
            {
                e.ToTable("buzones_correo");
                e.Property(b => b.Codigo).HasMaxLength(20);
                e.Property(b => b.ConfirmacionGmailCodigo).HasMaxLength(20);
                e.Property(b => b.ConfirmacionGmailEnlace).HasMaxLength(1000);
                e.HasIndex(b => b.UsuarioId).IsUnique();
                e.HasIndex(b => b.Codigo).IsUnique();
                e.HasOne<Usuario>().WithMany().HasForeignKey(b => b.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CorreoRecibido>(e =>
            {
                e.ToTable("correos_recibidos");
                e.Property(c => c.Remitente).HasMaxLength(254);
                e.Property(c => c.Asunto).HasMaxLength(300);
                e.Property(c => c.Detalle).HasMaxLength(500);
                e.Property(c => c.Estado).HasConversion<string>().HasMaxLength(20);
                e.HasIndex(c => new { c.UsuarioId, c.RecibidoEn });
                e.HasIndex(c => c.Estado);
                e.HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AvisoCuenta>(e =>
            {
                e.ToTable("avisos_cuenta");
                e.Property(a => a.Titulo).HasMaxLength(200);
                e.Property(a => a.Detalle).HasMaxLength(1000);
                e.Property(a => a.Ip).HasMaxLength(64);
                e.Property(a => a.Dispositivo).HasMaxLength(100);
                e.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(30);
                e.HasIndex(a => new { a.UsuarioId, a.Fecha });
                e.HasOne<Usuario>().WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FirmaElectronica>(e =>
            {
                e.ToTable("firmas_electronicas");
                e.Property(f => f.Titular).HasMaxLength(300);
                e.Property(f => f.Identificacion).HasMaxLength(13);
                e.Property(f => f.Emisor).HasMaxLength(300);
                e.Property(f => f.NumeroSerie).HasMaxLength(100);
                e.HasIndex(f => f.UsuarioId).IsUnique();
                e.HasOne<Usuario>().WithMany().HasForeignKey(f => f.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DatosEmisor>(e =>
            {
                e.ToTable("datos_emisor");
                e.Property(d => d.Ambiente).HasConversion<string>().HasMaxLength(15);
                e.Property(d => d.Establecimiento).HasMaxLength(3).IsFixedLength();
                e.Property(d => d.PuntoEmision).HasMaxLength(3).IsFixedLength();
                e.Property(d => d.NombreComercial).HasMaxLength(300);
                e.Property(d => d.DireccionMatriz).HasMaxLength(300);
                e.Property(d => d.DireccionEstablecimiento).HasMaxLength(300);
                e.Property(d => d.ContribuyenteEspecial).HasMaxLength(13);
                e.HasIndex(d => d.UsuarioId).IsUnique();
                e.HasOne<Usuario>().WithMany().HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FacturaEmitida>(e =>
            {
                e.Property(f => f.TipoDocumento).HasMaxLength(2).HasDefaultValue(TiposComprobante.Factura);
                e.Property(f => f.NumeroDocumentoSustento).HasMaxLength(20); e.Property(f => f.Motivo).HasMaxLength(300); e.Property(f => f.CodigoSustento).HasMaxLength(2);
                e.HasOne<FacturaEmitida>().WithMany().HasForeignKey(f => f.DocumentoSustentoId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Comprobante>().WithMany().HasForeignKey(f => f.ComprobanteSustentoId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(f => f.Retenciones).WithOne().HasForeignKey(d => d.FacturaEmitidaId).OnDelete(DeleteBehavior.Cascade);
                e.ToTable("facturas_emitidas");
                e.Property(f => f.Ambiente).HasConversion<string>().HasMaxLength(15);
                e.Property(f => f.ClaveAcceso).HasMaxLength(49).IsFixedLength();
                e.Property(f => f.Establecimiento).HasMaxLength(3).IsFixedLength();
                e.Property(f => f.PuntoEmision).HasMaxLength(3).IsFixedLength();
                e.Property(f => f.TipoIdentificacionComprador).HasMaxLength(2).IsFixedLength();
                e.Property(f => f.IdentificacionComprador).HasMaxLength(20);
                e.Property(f => f.RazonSocialComprador).HasMaxLength(300);
                e.Property(f => f.CorreoComprador).HasMaxLength(150);
                e.Property(f => f.DireccionComprador).HasMaxLength(300);
                e.Property(f => f.FormaPago).HasMaxLength(2).IsFixedLength();
                e.Property(f => f.Estado).HasConversion<string>().HasMaxLength(15);
                e.Property(f => f.Mensajes).HasMaxLength(2000);
                e.Property(f => f.NumeroAutorizacion).HasMaxLength(49);
                foreach (var monto in new[] { "Subtotal", "Descuento", "Iva", "Total" })
                    e.Property(monto).HasPrecision(12, 2);
                e.Ignore(f => f.Numero);

                e.HasIndex(f => f.ClaveAcceso).IsUnique();
                // Un mismo número de factura no puede repetirse en el mismo ambiente.
                e.HasIndex(f => new { f.UsuarioId, f.Ambiente, f.Establecimiento, f.PuntoEmision, f.Secuencial }).IsUnique();
                e.HasIndex(f => new { f.UsuarioId, f.CreadaEn });
                e.HasOne<Usuario>().WithMany().HasForeignKey(f => f.UsuarioId).OnDelete(DeleteBehavior.Cascade);
                e.HasMany(f => f.Detalles).WithOne().HasForeignKey(d => d.FacturaEmitidaId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FacturaEmitidaDetalle>(e =>
            {
                e.ToTable("facturas_emitidas_detalle");
                e.Property(d => d.Codigo).HasMaxLength(25);
                e.Property(d => d.Descripcion).HasMaxLength(300);
                e.Property(d => d.Cantidad).HasPrecision(18, 6);
                e.Property(d => d.PrecioUnitario).HasPrecision(18, 6);
                e.Property(d => d.Descuento).HasPrecision(12, 2);
                e.Property(d => d.Subtotal).HasPrecision(12, 2);
                e.Property(d => d.Iva).HasPrecision(12, 2);
                e.Property(d => d.Tarifa).HasConversion<string>().HasMaxLength(10);
            });

            modelBuilder.Entity<ServicioFacturable>(e =>
            {
                e.ToTable("servicios_facturables");
                e.Property(s => s.Codigo).HasMaxLength(25);
                e.Property(s => s.Descripcion).HasMaxLength(300);
                e.Property(s => s.PrecioUnitario).HasPrecision(18, 6);
                e.Property(s => s.Tarifa).HasConversion<string>().HasMaxLength(10);
                e.HasIndex(s => new { s.UsuarioId, s.Descripcion });
                e.HasOne<Usuario>().WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Suscripcion>(e =>
            {
                e.ToTable("suscripciones");
                e.Property(s => s.Plan).HasConversion<string>().HasMaxLength(20);
                e.Property(s => s.Periodicidad).HasConversion<string>().HasMaxLength(10);
                e.Property(s => s.Estado).HasConversion<string>().HasMaxLength(20);
                e.Property(s => s.KushkiId).HasMaxLength(100);
                e.Property(s => s.MarcaTarjeta).HasMaxLength(20);
                e.Property(s => s.UltimosDigitos).HasMaxLength(4);
                foreach (var monto in new[] { "Subtotal", "Iva", "Total" })
                    e.Property(monto).HasPrecision(12, 2);
                e.Ignore(s => s.Vigente);
                e.HasIndex(s => s.KushkiId).IsUnique();
                e.HasIndex(s => new { s.UsuarioId, s.Estado });
                e.HasOne<Usuario>().WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PagoSuscripcion>(e =>
            {
                e.ToTable("pagos_suscripcion");
                e.Property(p => p.Monto).HasPrecision(12, 2);
                e.Property(p => p.Ticket).HasMaxLength(100);
                e.Property(p => p.Detalle).HasMaxLength(500);
                e.HasIndex(p => p.Ticket).IsUnique();
                e.HasOne<Suscripcion>().WithMany().HasForeignKey(p => p.SuscripcionId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProveedorCategoria>(e =>
            {
                e.ToTable("proveedores_categoria");
                e.Property(p => p.Ruc).HasMaxLength(13);
                e.Property(p => p.Categoria).HasConversion<string>().HasMaxLength(20);
                e.HasIndex(p => new { p.UsuarioId, p.Ruc }).IsUnique();
                e.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
