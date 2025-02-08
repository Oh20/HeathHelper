using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Agenda> Agendas { get; set; }
    public DbSet<Consulta> Consultas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agenda>(entity =>
        {
            entity.ToTable("Agendas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MedicoId).IsRequired();
            entity.Property(e => e.DataHoraInicio).IsRequired();
            entity.Property(e => e.DataHoraFim).IsRequired();
            entity.Property(e => e.Disponivel).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.TipoAgenda)
                .IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.Observacao).HasMaxLength(500);
            entity.Property(e => e.Especialidade).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ValorConsulta).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Consulta>(entity =>
        {
            entity.ToTable("Consultas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AgendaId).IsRequired();
            entity.Property(e => e.PacienteId).IsRequired();
            entity.Property(e => e.MedicoId).IsRequired();
            entity.Property(e => e.DataSolicitacao).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.MotivoCancelamento).HasMaxLength(500);
            entity.Property(e => e.TipoAtendimento)
                .IsRequired()
                .HasConversion<string>();
            entity.Property(e => e.Especialidade).IsRequired().HasMaxLength(100);

            entity.HasOne<Agenda>()
                .WithMany()
                .HasForeignKey(c => c.AgendaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}