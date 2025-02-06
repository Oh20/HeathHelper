public class AgendaUpdateDto
{
    public int AgendaId { get; set; }
    public int MedicoId { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public string Especialidade { get; set; }
    public string? Observacao { get; set; }
    public TipoAgenda TipoAgenda { get; set; }
}