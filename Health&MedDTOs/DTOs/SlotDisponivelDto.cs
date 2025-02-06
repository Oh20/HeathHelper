public class SlotDisponivelDto
{
    public int AgendaId { get; set; }
    public int MedicoId { get; set; }
    public string NomeMedico { get; set; }
    public string CRM { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public string Especialidade { get; set; }
    public decimal ValorConsulta { get; set; }
    public string? Observacao { get; set; }
}
