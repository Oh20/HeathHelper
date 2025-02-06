using System.ComponentModel.DataAnnotations;

public class LoginRequestDto
{
    [Required]
    public string Identificacao { get; set; }

    [Required]
    public string Senha { get; set; }

    public string? CRM { get; set; }  // Opcional, usado apenas para médicos
}