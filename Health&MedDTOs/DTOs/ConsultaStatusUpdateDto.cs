public class ConsultaStatusUpdateDto
{
    public int ConsultaId { get; set; }
    public int MedicoId { get; set; }
    public StatusConsulta NovoStatus { get; set; }
    public string? Observacoes { get; set; }
    public string? MotivoCancelamento { get; set; }
}