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
[Route("api/barbeiros")]
public class BarbeirosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly LogAdminService _logAdminService;

    public BarbeirosController(AppDbContext db, LogAdminService logAdminService)
    {
        _db = db;
        _logAdminService = logAdminService;
    }

    // Público: qualquer um pode ver os barbeiros disponíveis
    [HttpGet]
    public async Task<IActionResult> ListarAtivos()
    {
        var barbeiros = await _db.Barbeiros
            .Where(b => b.Ativo)
            .Select(b => new BarbeiroResponseDto(b.Id, b.Nome, b.Telefone, b.Ativo, b.FotoBase64))
            .ToListAsync();

        return Ok(barbeiros);
    }

    // Admin: ver todos incluindo inativos
    [HttpGet("todos")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListarTodos()
    {
        var barbeiros = await _db.Barbeiros
            .Select(b => new BarbeiroResponseDto(b.Id, b.Nome, b.Telefone, b.Ativo, b.FotoBase64))
            .ToListAsync();

        return Ok(barbeiros);
    }

    // Admin: cadastrar novo barbeiro
    private const int TamanhoMaximoFotoBytes = 2 * 1024 * 1024; // 2 MB

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Criar([FromBody] BarbeiroRequestDto dto)
    {
        if (!string.IsNullOrEmpty(dto.FotoBase64) && !FotoValida(dto.FotoBase64, out var erroFoto))
            return BadRequest(new { erro = erroFoto });

        var barbeiro = new Barbeiro
        {
            Nome = dto.Nome,
            Telefone = dto.Telefone,
            FotoBase64 = dto.FotoBase64
        };

        _db.Barbeiros.Add(barbeiro);
        await _db.SaveChangesAsync();

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adminNome = User.FindFirstValue(ClaimTypes.Name)!;
        await _logAdminService.RegistrarAsync(adminId, adminNome, "CriouBarbeiro", "Barbeiro", barbeiro.Id,
            $"Nome: {barbeiro.Nome}");

        return CreatedAtAction(nameof(ListarAtivos),
            new BarbeiroResponseDto(barbeiro.Id, barbeiro.Nome, barbeiro.Telefone, barbeiro.Ativo, barbeiro.FotoBase64));
    }

    // Admin: editar barbeiro
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Editar(int id, [FromBody] BarbeiroRequestDto dto)
    {
        var barbeiro = await _db.Barbeiros.FindAsync(id);
        if (barbeiro == null)
            return NotFound(new { erro = "Barbeiro não encontrado." });

        if (!string.IsNullOrEmpty(dto.FotoBase64) && !FotoValida(dto.FotoBase64, out var erroFoto))
            return BadRequest(new { erro = erroFoto });

        barbeiro.Nome = dto.Nome;
        barbeiro.Telefone = dto.Telefone;
        barbeiro.FotoBase64 = dto.FotoBase64;

        await _db.SaveChangesAsync();

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adminNome = User.FindFirstValue(ClaimTypes.Name)!;
        await _logAdminService.RegistrarAsync(adminId, adminNome, "EditouBarbeiro", "Barbeiro", barbeiro.Id,
            $"Nome: {barbeiro.Nome}");

        return Ok(new BarbeiroResponseDto(barbeiro.Id, barbeiro.Nome, barbeiro.Telefone, barbeiro.Ativo, barbeiro.FotoBase64));
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

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adminNome = User.FindFirstValue(ClaimTypes.Name)!;
        await _logAdminService.RegistrarAsync(adminId, adminNome, "DesativouBarbeiro", "Barbeiro", barbeiro.Id,
            $"Nome: {barbeiro.Nome}");

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

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adminNome = User.FindFirstValue(ClaimTypes.Name)!;
        await _logAdminService.RegistrarAsync(adminId, adminNome, "AtivouBarbeiro", "Barbeiro", barbeiro.Id,
            $"Nome: {barbeiro.Nome}");

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

        if (dto.PausaInicio.HasValue != dto.PausaFim.HasValue)
            return BadRequest(new { erro = "Informe início e fim da pausa juntos, ou nenhum dos dois." });

        if (dto.PausaInicio.HasValue)
        {
            if (dto.PausaInicio >= dto.PausaFim)
                return BadRequest(new { erro = "Início da pausa deve ser antes do fim da pausa." });

            if (dto.PausaInicio < dto.HoraInicio || dto.PausaFim > dto.HoraFim)
                return BadRequest(new { erro = "A pausa deve estar dentro do expediente." });
        }

        var disponibilidade = new Disponibilidade
        {
            BarbeiroId = id,
            DiaSemana = dto.DiaSemana,
            HoraInicio = dto.HoraInicio,
            HoraFim = dto.HoraFim,
            PausaInicio = dto.PausaInicio,
            PausaFim = dto.PausaFim
        };

        _db.Disponibilidades.Add(disponibilidade);
        await _db.SaveChangesAsync();

        return Ok(new DisponibilidadeResponseDto(disponibilidade.Id, disponibilidade.DiaSemana,
            disponibilidade.HoraInicio, disponibilidade.HoraFim, disponibilidade.PausaInicio, disponibilidade.PausaFim));
    }

    // Público: ver disponibilidade de um barbeiro
    [HttpGet("{id}/disponibilidades")]
    public async Task<IActionResult> ListarDisponibilidades(int id)
    {
        var disponibilidades = await _db.Disponibilidades
            .Where(d => d.BarbeiroId == id)
            .Select(d => new DisponibilidadeResponseDto(d.Id, d.DiaSemana, d.HoraInicio, d.HoraFim, d.PausaInicio, d.PausaFim))
            .ToListAsync();

        return Ok(disponibilidades);
    }

    // Admin: editar uma disponibilidade específica
    [HttpPut("{barbeiroId}/disponibilidades/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditarDisponibilidade(int barbeiroId, int id, [FromBody] DisponibilidadeRequestDto dto)
    {
        var disponibilidade = await _db.Disponibilidades
            .FirstOrDefaultAsync(d => d.Id == id && d.BarbeiroId == barbeiroId);

        if (disponibilidade == null)
            return NotFound(new { erro = "Disponibilidade não encontrada." });

        if (dto.HoraInicio >= dto.HoraFim)
            return BadRequest(new { erro = "Hora de início deve ser antes da hora de fim." });

        if (dto.PausaInicio.HasValue != dto.PausaFim.HasValue)
            return BadRequest(new { erro = "Informe início e fim da pausa juntos, ou nenhum dos dois." });

        if (dto.PausaInicio.HasValue)
        {
            if (dto.PausaInicio >= dto.PausaFim)
                return BadRequest(new { erro = "Início da pausa deve ser antes do fim da pausa." });

            if (dto.PausaInicio < dto.HoraInicio || dto.PausaFim > dto.HoraFim)
                return BadRequest(new { erro = "A pausa deve estar dentro do expediente." });
        }

        disponibilidade.DiaSemana = dto.DiaSemana;
        disponibilidade.HoraInicio = dto.HoraInicio;
        disponibilidade.HoraFim = dto.HoraFim;
        disponibilidade.PausaInicio = dto.PausaInicio;
        disponibilidade.PausaFim = dto.PausaFim;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { erro = "Já existe uma disponibilidade cadastrada para esse dia da semana." });
        }

        return Ok(new DisponibilidadeResponseDto(disponibilidade.Id, disponibilidade.DiaSemana,
            disponibilidade.HoraInicio, disponibilidade.HoraFim, disponibilidade.PausaInicio, disponibilidade.PausaFim));
    }

    // Admin: remover uma disponibilidade específica
    [HttpDelete("{barbeiroId}/disponibilidades/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoverDisponibilidade(int barbeiroId, int id)
    {
        var disponibilidade = await _db.Disponibilidades
            .FirstOrDefaultAsync(d => d.Id == id && d.BarbeiroId == barbeiroId);

        if (disponibilidade == null)
            return NotFound(new { erro = "Disponibilidade não encontrada." });

        _db.Disponibilidades.Remove(disponibilidade);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private bool FotoValida(string fotoBase64, out string erro)
    {
        erro = string.Empty;

        if (!fotoBase64.StartsWith("data:image/"))
        {
            erro = "Formato de imagem inválido.";
            return false;
        }

        var partes = fotoBase64.Split(',');
        if (partes.Length != 2)
        {
            erro = "Formato de imagem inválido.";
            return false;
        }

        var tamanhoBytes = (partes[1].Length * 3) / 4;
        if (tamanhoBytes > TamanhoMaximoFotoBytes)
        {
            erro = "A imagem deve ter no máximo 2 MB.";
            return false;
        }

        return true;
    }
}