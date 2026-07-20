using BarberShopApi.Data;
using BarberShopApi.DTOs;
using BarberShopApi.Models;
using BarberShopApi.Services;
using BarberShopApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BarberShopApi.Controllers;

[ApiController]
[Route("api/agendamentos")]
public class AgendamentosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AgendamentoService _agendamentoService;
    private readonly EmailService _emailService;

    public AgendamentosController(AppDbContext db, AgendamentoService agendamentoService, EmailService emailService)
    {
        _db = db;
        _agendamentoService = agendamentoService;
        _emailService = emailService;
    }

    // Público: ver horários disponíveis de um barbeiro numa data
    [HttpGet("horarios-disponiveis")]
    public async Task<IActionResult> HorariosDisponiveis(
        [FromQuery] int barbeiroId,
        [FromQuery] int servicoId,
        [FromQuery] DateOnly data)
    {
        if (data < DateOnly.FromDateTime(TimeHelper.AgoraBrasil()))
            return BadRequest(new { erro = "Não é possível consultar horários em datas passadas." });

        var horarios = await _agendamentoService.GetHorariosDisponiveis(barbeiroId, servicoId, data);
        return Ok(horarios);
    }

    // Cliente autenticado: criar agendamento
    [HttpPost]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Criar([FromBody] CriarAgendamentoDto dto)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var servico = await _db.Servicos.FindAsync(dto.ServicoId);
        if (servico == null || !servico.Ativo)
            return BadRequest(new { erro = "Serviço inválido." });

        var barbeiro = await _db.Barbeiros.FindAsync(dto.BarbeiroId);
        if (barbeiro == null || !barbeiro.Ativo)
            return BadRequest(new { erro = "Barbeiro inválido." });

        if (dto.DataHoraInicio < TimeHelper.AgoraBrasil())
            return BadRequest(new { erro = "Não é possível agendar em data/hora passada." });

        var dataHoraInicio = DateTime.SpecifyKind(dto.DataHoraInicio, DateTimeKind.Unspecified);
        var dataHoraFim = dataHoraInicio.AddMinutes(servico.DuracaoMinutos);

        var agendamento = new Agendamento
        {
            UsuarioId = usuarioId,
            BarbeiroId = dto.BarbeiroId,
            ServicoId = dto.ServicoId,
            DataHoraInicio = dataHoraInicio,
            DataHoraFim = dataHoraFim,
            Status = "Pendente",
            TokenCancelamento = Guid.NewGuid().ToString()
        };

        _db.Agendamentos.Add(agendamento);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A constraint única do banco impediu o agendamento duplicado
            return Conflict(new { erro = "Esse horário acabou de ser reservado por outra pessoa. Escolha outro horário." });
        }

        // Envia e-mail de confirmação (não bloqueia o fluxo se falhar)
        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario != null)
        {
            var dataFormatada = dataHoraInicio.ToString("dd/MM/yyyy 'às' HH:mm");
            var corpoHtml = $@"
                <h2>Agendamento solicitado!</h2>
                <p>Olá, {usuario.Nome}!</p>
                <p>Seu agendamento na <strong>RZR Barber Shop</strong> foi recebido com sucesso:</p>
                <ul>
                    <li><strong>Serviço:</strong> {servico.Nome}</li>
                    <li><strong>Barbeiro:</strong> {barbeiro.Nome}</li>
                    <li><strong>Data/Hora:</strong> {dataFormatada}</li>
                </ul>
                <p>Aguarde a confirmação do barbeiro. Você receberá um novo e-mail assim que for confirmado.</p>
            ";

            await _emailService.EnviarAsync(usuario.Email, "Agendamento recebido - RZR Barber Shop", corpoHtml);
        }

        return CreatedAtAction(nameof(MeusAgendamentos), new { id = agendamento.Id },
            new { agendamento.Id, mensagem = "Agendamento criado com sucesso!" });
    }

    // Cliente autenticado: ver os próprios agendamentos
    [HttpGet("meus")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> MeusAgendamentos()
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var agendamentos = await _db.Agendamentos
            .Where(a => a.UsuarioId == usuarioId)
            .Include(a => a.Barbeiro)
            .Include(a => a.Servico)
            .Include(a => a.Usuario)
            .OrderByDescending(a => a.DataHoraInicio)
            .Select(a => new AgendamentoResponseDto(
                a.Id, a.Barbeiro.Nome, a.Servico.Nome,
                a.DataHoraInicio, a.DataHoraFim, a.Status, a.Usuario.Nome))
            .ToListAsync();

        return Ok(agendamentos);
    }

    // Cliente autenticado: cancelar o próprio agendamento
    [HttpPatch("{id}/cancelar")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var agendamento = await _db.Agendamentos
            .Include(a => a.Usuario)
            .Include(a => a.Servico)
            .Include(a => a.Barbeiro)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamento == null)
            return NotFound(new { erro = "Agendamento não encontrado." });

        if (agendamento.UsuarioId != usuarioId)
            return Forbid();

        agendamento.Status = "Cancelado";
        await _db.SaveChangesAsync();

        var dataFormatada = agendamento.DataHoraInicio.ToString("dd/MM/yyyy 'às' HH:mm");
        var corpoHtml = $@"
            <h2>Agendamento cancelado</h2>
            <p>Olá, {agendamento.Usuario.Nome}!</p>
            <p>Seu agendamento foi cancelado com sucesso:</p>
            <ul>
                <li><strong>Serviço:</strong> {agendamento.Servico.Nome}</li>
                <li><strong>Barbeiro:</strong> {agendamento.Barbeiro.Nome}</li>
                <li><strong>Data/Hora:</strong> {dataFormatada}</li>
            </ul>
        ";
        await _emailService.EnviarAsync(agendamento.Usuario.Email, "Agendamento cancelado - RZR Barber Shop", corpoHtml);

        return NoContent();
    }

    // Admin: ver todos os agendamentos
    [HttpGet("admin/todos")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TodosAgendamentos()
    {
        var agendamentos = await _db.Agendamentos
            .Include(a => a.Barbeiro)
            .Include(a => a.Servico)
            .Include(a => a.Usuario)
            .OrderByDescending(a => a.DataHoraInicio)
            .Select(a => new AgendamentoResponseDto(
                a.Id, a.Barbeiro.Nome, a.Servico.Nome,
                a.DataHoraInicio, a.DataHoraFim, a.Status, a.Usuario.Nome))
            .ToListAsync();

        return Ok(agendamentos);
    }

    // Admin: confirmar ou cancelar um agendamento
    [HttpPatch("admin/{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AtualizarStatus(int id, [FromBody] AtualizarStatusDto dto)
    {
        var agendamento = await _db.Agendamentos
            .Include(a => a.Usuario)
            .Include(a => a.Servico)
            .Include(a => a.Barbeiro)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamento == null)
            return NotFound(new { erro = "Agendamento não encontrado." });

        if (dto.Status != "Confirmado" && dto.Status != "Cancelado")
            return BadRequest(new { erro = "Status inválido. Use 'Confirmado' ou 'Cancelado'." });

        agendamento.Status = dto.Status;
        await _db.SaveChangesAsync();

        // Envia e-mail de acordo com o novo status
        var dataFormatada = agendamento.DataHoraInicio.ToString("dd/MM/yyyy 'às' HH:mm");

        if (dto.Status == "Confirmado")
        {
            var corpoHtml = $@"
            <h2>Agendamento confirmado!</h2>
            <p>Olá, {agendamento.Usuario.Nome}!</p>
            <p>Seu agendamento foi <strong>confirmado</strong> pelo barbeiro:</p>
            <ul>
                <li><strong>Serviço:</strong> {agendamento.Servico.Nome}</li>
                <li><strong>Barbeiro:</strong> {agendamento.Barbeiro.Nome}</li>
                <li><strong>Data/Hora:</strong> {dataFormatada}</li>
            </ul>
            <p>Te esperamos na RZR Barber Shop!</p>
        ";
            await _emailService.EnviarAsync(agendamento.Usuario.Email, "Agendamento confirmado - RZR Barber Shop", corpoHtml);
        }
        else if (dto.Status == "Cancelado")
        {
            var corpoHtml = $@"
            <h2>Agendamento cancelado</h2>
            <p>Olá, {agendamento.Usuario.Nome}!</p>
            <p>Seu agendamento foi <strong>cancelado</strong> pelo barbeiro:</p>
            <ul>
                <li><strong>Serviço:</strong> {agendamento.Servico.Nome}</li>
                <li><strong>Barbeiro:</strong> {agendamento.Barbeiro.Nome}</li>
                <li><strong>Data/Hora:</strong> {dataFormatada}</li>
            </ul>
            <p>Se quiser reagendar, é só acessar o site novamente.</p>
        ";
            await _emailService.EnviarAsync(agendamento.Usuario.Email, "Agendamento cancelado - RZR Barber Shop", corpoHtml);
        }

        return NoContent();
    }
}