using System.ComponentModel.DataAnnotations;

public class NovaConsultaDto
{
    [Required]
    public int AgendaId { get; set; }
    [Required]
    public int PacienteId { get; set; }
    public string? Observacoes { get; set; }
}