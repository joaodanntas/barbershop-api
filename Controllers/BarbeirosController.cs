using BarberShopApi.Data;
using BarberShopApi.DTOs;
using BarberShopApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberShopApi.Controllers;

[ApiController]
[Route("api/barbeiros")]
public class BarbeirosController : ControllerBase
{
    private readonly AppDbContext _db;

    public BarbeirosController(AppDbContext db)
    {
        _db = db;
    }

    // Público: qualquer um pode ver os barbeiros disponíveis
    [HttpGet]
    public async Task<IActionResult> ListarAtivos()
    {
        var barbeiros = await _db.Barbeiros
            .Where(b => b.Ativo)
            .Select(b => new BarbeiroResponseDto(b.Id, b.Nome, b.Telefone, b.Ativo))
            .ToListAsync();

        return Ok(barbeiros);
    }

    // Admin: ver todos incluindo inativos
    [HttpGet("todos")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListarTodos()
    {
        var barbeiros = await _db.Barbeiros
            .Select(b => new BarbeiroResponseDto(b.Id, b.Nome, b.Telefone, b.Ativo))
            .ToListAsync();

        return Ok(barbeiros);
    }

    // Admin: cadastrar novo barbeiro
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Criar([FromBody] BarbeiroRequestDto dto)
    {
        var barbeiro = new Barbeiro
        {
            Nome = dto.Nome,
            Telefone = dto.Telefone
        };

        _db.Barbeiros.Add(barbeiro);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ListarAtivos),
            new BarbeiroResponseDto(barbeiro.Id, barbeiro.Nome, barbeiro.Telefone, barbeiro.Ativo));
    }

    // Admin: editar barbeiro
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Editar(int id, [FromBody] BarbeiroRequestDto dto)
    {
        var barbeiro = await _db.Barbeiros.FindAsync(id);
        if (barbeiro == null)
            return NotFound(new { erro = "Barbeiro não encontrado." });

        barbeiro.Nome = dto.Nome;
        barbeiro.Telefone = dto.Telefone;

        await _db.SaveChangesAsync();

        return Ok(new BarbeiroResponseDto(barbeiro.Id, barbeiro.Nome, barbeiro.Telefone, barbeiro.Ativo));
    }

    // Admin: desativar barbeiro (nunca deletar, pois tem agendamentos vinculados)
    [HttpPatch("{id}/desativar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Desativar(int id)
    {
        var barbeiro = await _db.Barbeiros.FindAsync(id);
        if (barbeiro == null)
            return NotFound(new { erro = "Barbeiro não encontrado." });

        barbeiro.Ativo = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Admin: reativar barbeiro
    [HttpPatch("{id}/ativar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Ativar(int id)
    {
        var barbeiro = await _db.Barbeiros.FindAsync(id);
        if (barbeiro == null)
            return NotFound(new { erro = "Barbeiro não encontrado." });

        barbeiro.Ativo = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Admin: cadastrar disponibilidade de um barbeiro
    [HttpPost("{id}/disponibilidades")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdicionarDisponibilidade(int id, [FromBody] DisponibilidadeRequestDto dto)
    {
        var barbeiro = await _db.Barbeiros.FindAsync(id);
        if (barbeiro == null)
            return NotFound(new { erro = "Barbeiro não encontrado." });

        if (dto.HoraInicio >= dto.HoraFim)
            return BadRequest(new { erro = "Hora de início deve ser antes da hora de fim." });

        var disponibilidade = new Disponibilidade
        {
            BarbeiroId = id,
            DiaSemana = dto.DiaSemana,
            HoraInicio = dto.HoraInicio,
            HoraFim = dto.HoraFim
        };

        _db.Disponibilidades.Add(disponibilidade);
        await _db.SaveChangesAsync();

        return Ok(new DisponibilidadeResponseDto(disponibilidade.Id, disponibilidade.DiaSemana, disponibilidade.HoraInicio, disponibilidade.HoraFim));
    }

    // Público: ver disponibilidade de um barbeiro
    [HttpGet("{id}/disponibilidades")]
    public async Task<IActionResult> ListarDisponibilidades(int id)
    {
        var disponibilidades = await _db.Disponibilidades
            .Where(d => d.BarbeiroId == id)
            .Select(d => new DisponibilidadeResponseDto(d.Id, d.DiaSemana, d.HoraInicio, d.HoraFim))
            .ToListAsync();

        return Ok(disponibilidades);
    }
}