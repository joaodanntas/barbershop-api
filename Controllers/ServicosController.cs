using BarberShopApi.Data;
using BarberShopApi.DTOs;
using BarberShopApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberShopApi.Controllers;

[ApiController]
[Route("api/servicos")]
public class ServicosController : ControllerBase
{
    private readonly AppDbContext _db;

    public ServicosController(AppDbContext db)
    {
        _db = db;
    }

    // Público: qualquer um pode ver os serviços disponíveis
    [HttpGet]
    public async Task<IActionResult> ListarAtivos()
    {
        var servicos = await _db.Servicos
            .Where(s => s.Ativo)
            .Select(s => new ServicoResponseDto(s.Id, s.Nome, s.DuracaoMinutos, s.Preco, s.Ativo))
            .ToListAsync();

        return Ok(servicos);
    }

    // Admin: cadastrar novo serviço
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Criar([FromBody] ServicoRequestDto dto)
    {
        if (dto.DuracaoMinutos <= 0)
            return BadRequest(new { erro = "Duração deve ser maior que zero." });

        if (dto.Preco <= 0)
            return BadRequest(new { erro = "Preço deve ser maior que zero." });

        var servico = new Servico
        {
            Nome = dto.Nome,
            DuracaoMinutos = dto.DuracaoMinutos,
            Preco = dto.Preco
        };

        _db.Servicos.Add(servico);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ListarAtivos),
            new ServicoResponseDto(servico.Id, servico.Nome, servico.DuracaoMinutos, servico.Preco, servico.Ativo));
    }

    // Admin: editar serviço
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Editar(int id, [FromBody] ServicoRequestDto dto)
    {
        var servico = await _db.Servicos.FindAsync(id);
        if (servico == null)
            return NotFound(new { erro = "Serviço não encontrado." });

        if (dto.DuracaoMinutos <= 0)
            return BadRequest(new { erro = "Duração deve ser maior que zero." });

        if (dto.Preco <= 0)
            return BadRequest(new { erro = "Preço deve ser maior que zero." });

        servico.Nome = dto.Nome;
        servico.DuracaoMinutos = dto.DuracaoMinutos;
        servico.Preco = dto.Preco;

        await _db.SaveChangesAsync();

        return Ok(new ServicoResponseDto(servico.Id, servico.Nome, servico.DuracaoMinutos, servico.Preco, servico.Ativo));
    }

    // Admin: desativar serviço
    [HttpPatch("{id}/desativar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Desativar(int id)
    {
        var servico = await _db.Servicos.FindAsync(id);
        if (servico == null)
            return NotFound(new { erro = "Serviço não encontrado." });

        servico.Ativo = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Admin: reativar serviço
    [HttpPatch("{id}/ativar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Ativar(int id)
    {
        var servico = await _db.Servicos.FindAsync(id);
        if (servico == null)
            return NotFound(new { erro = "Serviço não encontrado." });

        servico.Ativo = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}