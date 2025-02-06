using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Medico> Medicos { get; set; }
    public DbSet<Paciente> Pacientes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medico>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CPF).IsRequired().HasMaxLength(11);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Senha).IsRequired();
            entity.Property(e => e.CRM).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Especialidade).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Ativo).HasDefaultValue(true);

            entity.HasIndex(e => e.CPF).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CRM).IsUnique();
        });

        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CPF).IsRequired().HasMaxLength(11);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Senha).IsRequired();
            entity.Property(e => e.Telefone).HasMaxLength(20);
            entity.Property(e => e.Convenio).HasMaxLength(100);
            entity.Property(e => e.NumeroConvenio).HasMaxLength(50);
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Ativo).HasDefaultValue(true);

            entity.HasIndex(e => e.CPF).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}