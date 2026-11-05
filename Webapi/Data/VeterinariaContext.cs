using Microsoft.EntityFrameworkCore;
using Webapi.Models;

namespace Webapi.Data;

public class VeterinariaContext(DbContextOptions<VeterinariaContext> options) : DbContext(options)
{
    public DbSet<Propietario> Propietarios => Set<Propietario>();
    public DbSet<Mascota> Mascotas => Set<Mascota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Propietario>(entity =>
        {
            entity.ToTable("Propietarios");
            entity.HasIndex(p => p.Dni).IsUnique().HasDatabaseName("UQ_Propietarios_Dni");
            entity.HasIndex(p => p.Email).IsUnique().HasDatabaseName("UQ_Propietarios_Email");
        });
        modelBuilder.Entity<Mascota>(entity =>
        {
            entity.ToTable("Mascotas");
            entity.Property(m => m.Peso).HasPrecision(5, 2);
            entity.HasOne(m => m.Propietario).WithMany(p => p.Mascotas)
                .HasForeignKey(m => m.IdPropietario).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Mascotas_Propietarios");
        });
    }
}
