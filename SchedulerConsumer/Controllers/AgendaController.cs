using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SchedulerConsumer.Controllers
{
    [ApiController]
    [Route("api/medicos")]
    public class MedicoController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<MedicoController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public MedicoController(
            ApplicationDbContext dbContext,
            ILogger<MedicoController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("{medicoId}/consultas")]
        public async Task<IActionResult> ListarConsultasMedico(int medicoId)
        {
            try
            {
                // Verifica se o médico existe
                var medico = await GetMedicoById(medicoId);
                if (medico == null)
                {
                    return NotFound("Médico não encontrado");
                }

                var consultas = await _dbContext.Consultas
                    .Where(c => c.MedicoId == medicoId)
                    .Select(c => new ConsultaDto
                    {
                        Id = c.Id,
                        AgendaId = c.AgendaId,
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

                return Ok(consultas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao listar consultas do médico {medicoId}");
                return StatusCode(500, "Erro ao buscar consultas");
            }
        }

        private async Task<MedicoDto?> GetMedicoById(int medicoId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("UserService");
                var response = await client.GetAsync($"/api/medicos/{medicoId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<MedicoDto>();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar dados do médico");
                return null;
            }
        }
    }

    [ApiController]
    [Route("api/slots")]
    public class SlotController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<SlotController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public SlotController(
            ApplicationDbContext dbContext,
            ILogger<SlotController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("disponiveis")]
        public async Task<IActionResult> GetSlotsDisponiveis(
            [FromQuery] string especialidade,
            [FromQuery] DateTime data,
            [FromQuery] int? medicoId = null,
            [FromQuery] decimal? valorMaximo = null)
        {
            try
            {
                var query = _dbContext.Agendas
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
                        DataHoraInicio = a.DataHoraInicio,
                        DataHoraFim = a.DataHoraFim,
                        Especialidade = a.Especialidade,
                        ValorConsulta = a.ValorConsulta,
                        Observacao = a.Observacao
                    })
                    .OrderBy(s => s.ValorConsulta)
                    .ToListAsync();

                // Busca informações adicionais dos médicos
                foreach (var slot in slots)
                {
                    var medico = await GetMedicoById(slot.MedicoId);
                    if (medico != null)
                    {
                        slot.NomeMedico = medico.Nome;
                        slot.CRM = medico.CRM;
                    }
                }

                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar slots disponíveis");
                return StatusCode(500, "Erro ao buscar slots disponíveis");
            }
        }

        private async Task<MedicoDto?> GetMedicoById(int medicoId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("UserService");
                var response = await client.GetAsync($"/api/medicos/{medicoId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<MedicoDto>();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar dados do médico");
                return null;
            }
        }
    }

    [ApiController]
    [Route("api/pacientes")]
    public class PacienteController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<PacienteController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public PacienteController(
            ApplicationDbContext dbContext,
            ILogger<PacienteController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("{pacienteId}/consultas")]
        public async Task<IActionResult> ListarConsultasPaciente(int pacienteId)
        {
            try
            {
                // Verifica se o paciente existe
                var paciente = await GetPacienteById(pacienteId);
                if (paciente == null)
                {
                    return NotFound("Paciente não encontrado");
                }

                var consultas = await _dbContext.Consultas
                    .Where(c => c.PacienteId == pacienteId)
                    .Select(c => new ConsultaDto
                    {
                        Id = c.Id,
                        AgendaId = c.AgendaId,
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

        private async Task<Paciente?> GetPacienteById(int pacienteId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("UserService");
                var response = await client.GetAsync($"/api/pacientes/{pacienteId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Paciente>();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar dados do paciente");
                return null;
            }
        }
    }
}