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
[Route("api/servicos")]
public class ServicosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly LogAdminService _logAdminService;

    public ServicosController(AppDbContext db, LogAdminService logAdminService)
    {
        _db = db;
        _logAdminService = logAdminService;
    }

    // Público: qualquer um pode ver os serviços disponíveis
    [HttpGet]
    public async Task<IActionResult> ListarAtivos()
    {
        var servicos = await _db.Servicos
            .Where(s => s.Ativo)
            .Select(s => new ServicoResponseDto(s.Id, s.Nome, s.DuracaoMinutos, s.Preco, s.Ativo, s.AntecedenciaMinimaMinutos))
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

        if (dto.AntecedenciaMinimaMinutos < 0)
            return BadRequest(new { erro = "Antecedência mínima não pode ser negativa." });

        var servico = new Servico
        {
            Nome = dto.Nome,
            DuracaoMinutos = dto.DuracaoMinutos,
            Preco = dto.Preco,
            AntecedenciaMinimaMinutos = dto.AntecedenciaMinimaMinutos
        };

        _db.Servicos.Add(servico);
        await _db.SaveChangesAsync();

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adminNome = User.FindFirstValue(ClaimTypes.Name)!;
        await _logAdminService.RegistrarAsync(adminId, adminNome, "CriouServico", "Servico", servico.Id,
            $"Nome: {servico.Nome} · Preço: {servico.Preco}");

        return CreatedAtAction(nameof(ListarAtivos),
            new ServicoResponseDto(servico.Id, servico.Nome, servico.DuracaoMinutos, servico.Preco, servico.Ativo, servico.AntecedenciaMinimaMinutos));
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

        if (dto.AntecedenciaMinimaMinutos < 0)
            return BadRequest(new { erro = "Antecedência mínima não pode ser negativa." });

        servico.Nome = dto.Nome;
        servico.DuracaoMinutos = dto.DuracaoMinutos;
        servico.Preco = dto.Preco;
        servico.AntecedenciaMinimaMinutos = dto.AntecedenciaMinimaMinutos;

        await _db.SaveChangesAsync();

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adminNome = User.FindFirstValue(ClaimTypes.Name)!;
        await _logAdminService.RegistrarAsync(adminId, adminNome, "EditouServico", "Servico", servico.Id,
            $"Nome: {servico.Nome} · Preço: {servico.Preco}");

        return Ok(new ServicoResponseDto(servico.Id, servico.Nome, servico.DuracaoMinutos, servico.Preco, servico.Ativo, servico.AntecedenciaMinimaMinutos));
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

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adminNome = User.FindFirstValue(ClaimTypes.Name)!;
        await _logAdminService.RegistrarAsync(adminId, adminNome, "DesativouServico", "Servico", servico.Id,
            $"Nome: {servico.Nome}");

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

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adminNome = User.FindFirstValue(ClaimTypes.Name)!;
        await _logAdminService.RegistrarAsync(adminId, adminNome, "AtivouServico", "Servico", servico.Id,
            $"Nome: {servico.Nome}");

        return NoContent();
    }
}