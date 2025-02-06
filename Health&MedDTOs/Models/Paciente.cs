public class Paciente : BaseUser
{
    public string? Telefone { get; set; }
    public DateTime DataNascimento { get; set; }
    public string? Convenio { get; set; }
    public string? NumeroConvenio { get; set; }
}