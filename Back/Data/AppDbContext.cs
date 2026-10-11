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
                e.HasIndex(c => c.Identificacion).IsUnique();
            });

            modelBuilder.Entity<Producto>(e =>
            {
                e.Property(p => p.Nombre).HasMaxLength(150);
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
