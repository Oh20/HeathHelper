public class Agendamento
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public int AgendaId { get; set; }
    public DateTime DataHora { get; set; }
    public StatusConsulta Status { get; set; }
    public virtual Paciente Paciente { get; set; }
}