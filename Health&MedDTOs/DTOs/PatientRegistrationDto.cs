 public class PatientRegistrationDto
    {
        public string CPF { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string? Telefone { get; set; }
        public DateTime DataNascimento { get; set; }
        public string? Convenio { get; set; }
        public string? NumeroConvenio { get; set; }
    }