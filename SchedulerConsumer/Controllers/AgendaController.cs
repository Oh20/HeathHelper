using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/medicos")]
public class MedicoController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MedicoController> _logger;

    public MedicoController(ApplicationDbContext dbContext, ILogger<MedicoController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("{medicoId}/consultas")]
    public async Task<IActionResult> ListarConsultasMedico(int medicoId)
    {
        try
        {
            // Primeiro, vamos logar a query
            _logger.LogInformation($"Buscando consultas para o médico {medicoId}");

            // Vamos verificar se existem consultas no banco
            var todasConsultas = await _dbContext.Consultas.ToListAsync();
            _logger.LogInformation($"Total de consultas no banco: {todasConsultas.Count}");

            var consultas = await _dbContext.Consultas
                .Where(c => c.MedicoId == medicoId)
                .Select(c => new ConsultaDto
                {
                    Id = c.Id,
                    PacienteId = c.PacienteId,
                    MedicoId = c.MedicoId,
                    DataConsulta = c.DataConsulta,
                    Status = c.Status,
                    Observacoes = c.Observacoes,
                    DataSolicitacao = c.DataSolicitacao,
                    DataConfirmacao = c.DataConfirmacao,
                    DataCancelamento = c.DataCancelamento,
                    MotivoCancelamento = c.MotivoCancelamento,
                    Especialidade = c.Especialidade
                })
                .ToListAsync();

            _logger.LogInformation($"Encontradas {consultas.Count} consultas para o médico {medicoId}");

            return Ok(consultas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao listar consultas do médico {medicoId}");
            return StatusCode(500, "Erro ao buscar consultas");
        }
    }
}

[ApiController]
[Route("api/slots")]
public class SlotController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SlotController> _logger;

    public SlotController(ApplicationDbContext dbContext, ILogger<SlotController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("slots/disponiveis")]
    public async Task<IActionResult> GetSlotsDisponiveis(
    [FromQuery] string especialidade,
    [FromQuery] DateTime data,
    [FromQuery] int? medicoId = null,
    [FromQuery] decimal? valorMaximo = null)
    {
        try
        {
            var query = _dbContext.Agendas
                .Include(a => a.Medico)
                .Where(a =>
                    a.Disponivel &&
                    a.DataHoraInicio.Date == data.Date &&
                    a.Especialidade == especialidade);

            if (medicoId.HasValue)
                query = query.Where(a => a.MedicoId == medicoId.Value);

            if (valorMaximo.HasValue)
                query = query.Where(a => a.ValorConsulta <= valorMaximo.Value);

            var slots = await query
                .Select(a => new SlotDisponivelDto
                {
                    AgendaId = a.Id,
                    MedicoId = a.MedicoId,
                    NomeMedico = a.Medico.Nome,
                    CRM = a.Medico.CRM,
                    DataHoraInicio = a.DataHoraInicio,
                    DataHoraFim = a.DataHoraFim,
                    Especialidade = a.Especialidade,
                    ValorConsulta = a.ValorConsulta,
                    Observacao = a.Observacao
                })
                    .OrderBy(s => s.ValorConsulta)
                .ToListAsync();

            return Ok(slots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar slots disponíveis");
            return StatusCode(500, "Erro ao buscar slots disponíveis");
        }
    }
}

[ApiController]
[Route("api/pacientes")]
public class PacienteController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PacienteController> _logger;

    public PacienteController(ApplicationDbContext dbContext, ILogger<PacienteController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("{pacienteId}/consultas")]
    public async Task<IActionResult> ListarConsultasPaciente(int pacienteId)
    {
        try
        {
            var consultas = await _dbContext.Consultas
                .Where(c => c.PacienteId == pacienteId)
                .Select(c => new ConsultaDto
                {
                    Id = c.Id,
                    MedicoId = c.MedicoId,
                    DataConsulta = c.DataConsulta,
                    Status = c.Status,
                    Observacoes = c.Observacoes,
                    DataSolicitacao = c.DataSolicitacao,
                    Especialidade = c.Especialidade
                })
                .OrderByDescending(c => c.DataConsulta)
                .ToListAsync();

            return Ok(consultas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar consultas do paciente");
            return StatusCode(500, "Erro interno ao buscar consultas");
        }
    }
}