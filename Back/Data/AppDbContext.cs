using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back
{
    public class AppDbContext : DbContext
    {
        public class AppDbContext : DbContext
        {
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public DbSet<Cliente> Clientes { get; set; }
            public DbSet<Usuario> Usuarios { get; set; }
            public DbSet<Producto> Productos { get; set; }
            public DbSet<Factura> Facturas { get; set; }
            public DbSet<DetalleFactura> DetallesFactura { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                // Configuraciones adicionales (relaciones, restricciones, etc.)
                base.OnModelCreating(modelBuilder);
            }
        }
    }
}
