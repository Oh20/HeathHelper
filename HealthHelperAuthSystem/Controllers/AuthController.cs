using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly TokenService _tokenService;
    private readonly IPasswordHasher<BaseUser> _passwordHasher;

    public AuthController(
        ApplicationDbContext context,
        TokenService tokenService,
        IPasswordHasher<BaseUser> passwordHasher)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
    {
        // Se CRM está presente, é login de médico
        if (!string.IsNullOrEmpty(loginDto.CRM))
        {
            var medico = await _context.Medicos
                .FirstOrDefaultAsync(m =>
                    m.Email == loginDto.Identificacao &&
                    m.CRM == loginDto.CRM);

            if (medico == null || !VerificarSenha(medico, loginDto.Senha))
                return Unauthorized("Credenciais inválidas");

            var token = _tokenService.GenerateToken(medico, "Medico");
            return Ok(new LoginResponseDto
            {
                Token = token,
                Nome = medico.Nome,
                TipoUsuario = "Medico",
                UsuarioId = medico.Id,
                Expiracao = DateTime.UtcNow.AddHours(8)
            });
        }

        // Login de paciente
        var paciente = await _context.Pacientes
            .FirstOrDefaultAsync(p =>
                p.Email == loginDto.Identificacao ||
                p.CPF == loginDto.Identificacao);

        if (paciente == null || !VerificarSenha(paciente, loginDto.Senha))
            return Unauthorized("Credenciais inválidas");

        var pacienteToken = _tokenService.GenerateToken(paciente, "Paciente");
        return Ok(new LoginResponseDto
        {
            Token = pacienteToken,
            Nome = paciente.Nome,
            TipoUsuario = "Paciente",
            UsuarioId = paciente.Id,
            Expiracao = DateTime.UtcNow.AddHours(8)
        });
    }

    private bool VerificarSenha(BaseUser user, string senha)
    {
        return _passwordHasher.VerifyHashedPassword(user, user.Senha, senha)
            != PasswordVerificationResult.Failed;
    }
}