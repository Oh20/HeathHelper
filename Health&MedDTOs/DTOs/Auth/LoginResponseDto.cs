public class LoginResponseDto
{
    public string Token { get; set; }
    public string Nome { get; set; }
    public string TipoUsuario { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Expiracao { get; set; }
}