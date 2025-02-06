public abstract class BaseUser
{
    public int Id { get; set; }
    public string CPF { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public string Senha { get; set; }
    public DateTime DataCriacao { get; set; }
    public bool Ativo { get; set; }
}