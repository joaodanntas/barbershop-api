using BarberShopApi.Data;
using BarberShopApi.DTOs;
using BarberShopApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberShopApi.Controllers;

[ApiController]
[Route("api/bloqueios")]
[Authorize(Roles = "Admin")]
public class BloqueiosDataController : ControllerBase
{
    private readonly AppDbContext _db;

    public BloqueiosDataController(AppDbContext db)
    {
        _db = db;
    }

    // Admin: listar todos os bloqueios (globais + individuais)
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var bloqueios = await _db.BloqueiosData
            .Include(bd => bd.Barbeiro)
            .OrderBy(bd => bd.Data)
            .Select(bd => new BloqueioDataResponseDto(
                bd.Id, bd.BarbeiroId, bd.Barbeiro != null ? bd.Barbeiro.Nome : null, bd.Data, bd.Motivo))
            .ToListAsync();

        return Ok(bloqueios);
    }

    // Admin: criar bloqueio (global ou por barbeiro)
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] BloqueioDataRequestDto dto)
    {
        if (dto.BarbeiroId.HasValue)
        {
            var barbeiro = await _db.Barbeiros.FindAsync(dto.BarbeiroId.Value);
            if (barbeiro == null)
                return BadRequest(new { erro = "Barbeiro inválido." });
        }

        var bloqueio = new BloqueioData
        {
            BarbeiroId = dto.BarbeiroId,
            Data = dto.Data,
            Motivo = dto.Motivo
        };

        _db.BloqueiosData.Add(bloqueio);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var alvo = dto.BarbeiroId.HasValue ? "esse barbeiro" : "todos os barbeiros (bloqueio global)";
            return Conflict(new { erro = $"Já existe um bloqueio para {alvo} nessa data." });
        }

        return CreatedAtAction(nameof(Listar), new { id = bloqueio.Id },
            new { bloqueio.Id, mensagem = "Bloqueio cadastrado com sucesso!" });
    }

    // Admin: remover bloqueio
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remover(int id)
    {
        var bloqueio = await _db.BloqueiosData.FindAsync(id);
        if (bloqueio == null)
            return NotFound(new { erro = "Bloqueio não encontrado." });

        _db.BloqueiosData.Remove(bloqueio);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}