public class Consulta
{
    public int Id { get; set; }
    public int AgendaId { get; set; }
    public int PacienteId { get; set; }
    public int MedicoId { get; set; }
    public DateTime DataConsulta { get; set; }
    public DateTime DataSolicitacao { get; set; }
    public StatusConsulta Status { get; set; }
    public string? Observacoes { get; set; }
    public DateTime? DataConfirmacao { get; set; }
    public DateTime? DataCancelamento { get; set; }
    public string? MotivoCancelamento { get; set; }
    public TipoAtendimento TipoAtendimento { get; set; }
    public string Especialidade { get; set; }  
}