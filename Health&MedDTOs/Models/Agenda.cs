using System.ComponentModel.DataAnnotations.Schema;

public class Agenda
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public bool Disponivel { get; set; }
    public string? Observacao { get; set; }
    public TipoAgenda TipoAgenda { get; set; }
    public string Especialidade { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorConsulta { get; set; }

    public virtual Medico Medico { get; set; }
}

