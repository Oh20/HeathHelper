using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class ConsultaUpdateDto
{
    public int ConsultaId { get; set; }
    public int MedicoId { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusConsulta NovoStatus { get; set; }

    public string? Observacoes { get; set; }
    public string? MotivoCancelamento { get; set; }
    public TipoAtendimento TipoAtendimento { get; set; }
    public DateTime? DataConfirmacao { get; set; }
    public DateTime? DataCancelamento { get; set; }
}