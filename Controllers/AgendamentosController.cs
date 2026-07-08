using BarberShopApi.Data;
using BarberShopApi.DTOs;
using BarberShopApi.Models;
using BarberShopApi.Services;
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

    public AgendamentosController(AppDbContext db, AgendamentoService agendamentoService)
    {
        _db = db;
        _agendamentoService = agendamentoService;
    }

    // Público: ver horários disponíveis de um barbeiro numa data
    [HttpGet("horarios-disponiveis")]
    public async Task<IActionResult> HorariosDisponiveis(
        [FromQuery] int barbeiroId,
        [FromQuery] int servicoId,
        [FromQuery] DateOnly data)
    {
        if (data < DateOnly.FromDateTime(DateTime.Today))
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

        if (dto.DataHoraInicio < DateTime.UtcNow)
            return BadRequest(new { erro = "Não é possível agendar em data/hora passada." });

        var dataHoraInicio = DateTime.SpecifyKind(dto.DataHoraInicio, DateTimeKind.Utc);
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

        var agendamento = await _db.Agendamentos.FindAsync(id);
        if (agendamento == null)
            return NotFound(new { erro = "Agendamento não encontrado." });

        if (agendamento.UsuarioId != usuarioId)
            return Forbid();

        agendamento.Status = "Cancelado";
        await _db.SaveChangesAsync();

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
        var agendamento = await _db.Agendamentos.FindAsync(id);
        if (agendamento == null)
            return NotFound(new { erro = "Agendamento não encontrado." });

        if (dto.Status != "Confirmado" && dto.Status != "Cancelado")
            return BadRequest(new { erro = "Status inválido. Use 'Confirmado' ou 'Cancelado'." });

        agendamento.Status = dto.Status;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}