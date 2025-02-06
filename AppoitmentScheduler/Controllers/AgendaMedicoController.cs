using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/medicos")]
public class MedicoController : ControllerBase
{
    private readonly AgendaProducer _agendaProducer;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MedicoController> _logger;

    public MedicoController(
        AgendaProducer agendaProducer,
        IHttpClientFactory httpClientFactory,
        ILogger<MedicoController> logger)
    {
        _agendaProducer = agendaProducer;
        _httpClient = httpClientFactory.CreateClient("ConsumerService");
        _logger = logger;
    }

    [HttpPost("{medicoId}/agenda/slots")]
    public IActionResult CriarSlots(int medicoId, [FromBody] AgendaInputDto[] slots)
    {
        try
        {
            foreach (var slot in slots)
            {
                var agendaDto = new AgendaDto
                {
                    MedicoId = medicoId,
                    DataHoraInicio = slot.DataHoraInicio,
                    DataHoraFim = slot.DataHoraFim,
                    TipoAgenda = slot.TipoAgenda,
                    Observacao = slot.Observacao,
                    Especialidade = slot.Especialidade
                };

                _agendaProducer.PublishAgenda(agendaDto);
            }

            return Ok(new { Message = "Slots de agenda enviados para processamento" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar slots de agenda");
            return StatusCode(500, "Erro ao processar slots de agenda");
        }
    }

    [HttpPut("{medicoId}/agenda/slots/{agendaId}")]
    public IActionResult AtualizarSlot(int medicoId, int agendaId, [FromBody] AgendaInputDto slot)
    {
        try
        {
            _agendaProducer.PublishAgendaUpdate(slot, medicoId, agendaId);
            return Ok(new { Message = "Atualização do slot enviada para processamento" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar slot de agenda");
            return StatusCode(500, "Erro ao processar atualização do slot");
        }
    }

    [HttpGet("{medicoId}/consultas")]
    public async Task<IActionResult> ListarConsultasMedico(int medicoId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/medicos/{medicoId}/consultas");
            if (response.IsSuccessStatusCode)
            {
                var consultas = await response.Content.ReadFromJsonAsync<List<ConsultaDto>>();
                return Ok(consultas);
            }
            return StatusCode((int)response.StatusCode, "Erro ao buscar consultas do médico");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar consultas do médico");
            return StatusCode(500, "Erro ao processar a requisição");
        }
    }
}

[ApiController]
[Route("api/agenda")]
public class AgendaController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AgendaController> _logger;

    public AgendaController(
        IHttpClientFactory httpClientFactory,
        ILogger<AgendaController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ConsumerService");
        _logger = logger;
    }

    [HttpGet("slots/disponiveis")]
    public async Task<IActionResult> ListarSlotsDisponiveis(
        [FromQuery] string especialidade,
        [FromQuery] DateTime data,
        [FromQuery] int? medicoId = null)
    {
        try
        {
            var queryString = $"especialidade={especialidade}&data={data:yyyy-MM-dd}";
            if (medicoId.HasValue)
            {
                queryString += $"&medicoId={medicoId.Value}";
            }

            var response = await _httpClient.GetAsync($"/api/slots/disponiveis?{queryString}");
            if (response.IsSuccessStatusCode)
            {
                var slots = await response.Content.ReadFromJsonAsync<List<SlotDisponivelDto>>();
                return Ok(slots);
            }

            return StatusCode((int)response.StatusCode, "Erro ao buscar slots disponíveis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar slots disponíveis");
            return StatusCode(500, "Erro ao processar a requisição");
        }
    }
}

[ApiController]
[Route("api/consultas")]
public class ConsultaController : ControllerBase
{
    private readonly AgendaProducer _agendaProducer;
    private readonly ILogger<ConsultaController> _logger;

    public ConsultaController(
        AgendaProducer agendaProducer,
        ILogger<ConsultaController> logger)
    {
        _agendaProducer = agendaProducer;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult AgendarConsulta([FromBody] NovaConsultaDto consultaDto)
    {
        try
        {
            _agendaProducer.PublishNovaConsulta(consultaDto);
            return Ok(new { Message = "Solicitação de consulta enviada para processamento" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao agendar consulta");
            return StatusCode(500, "Erro ao processar agendamento");
        }
    }

    [HttpPut("{consultaId}/status")]
    public IActionResult AtualizarStatusConsulta(
        int consultaId,
        [FromQuery] int medicoId,
        [FromBody] ConsultaUpdateDto updateDto)
    {
        try
        {
            if (updateDto == null)
                return BadRequest("Dados de atualização inválidos");

            _agendaProducer.PublishConsultaUpdate(medicoId, consultaId, updateDto);
            return Ok(new { Message = "Atualização da consulta enviada para processamento" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar status da consulta");
            return StatusCode(500, "Erro ao processar atualização da consulta");
        }
    }
}

[ApiController]
[Route("api/pacientes")]
public class PacienteController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PacienteController> _logger;

    public PacienteController(
        IHttpClientFactory httpClientFactory,
        ILogger<PacienteController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ConsumerService");
        _logger = logger;
    }

    [HttpGet("{pacienteId}/consultas")]
    public async Task<IActionResult> ListarConsultasPaciente(int pacienteId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/pacientes/{pacienteId}/consultas");
            if (response.IsSuccessStatusCode)
            {
                var consultas = await response.Content.ReadFromJsonAsync<List<ConsultaDto>>();
                return Ok(consultas);
            }

            return StatusCode((int)response.StatusCode, "Erro ao buscar consultas");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar consultas do paciente");
            return StatusCode(500, "Erro ao processar a requisição");
        }
    }
}