using BarberShopApi.Data;
using BarberShopApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BarberShopApi.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsuariosController(AppDbContext db)
    {
        _db = db;
    }

    // Ver os próprios dados
    [HttpGet("me")]
    public async Task<IActionResult> Meu()
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario == null) return NotFound();

        return Ok(new MeuPerfilDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Telefone));
    }

    // Editar os próprios dados
    [HttpPut("me")]
    public async Task<IActionResult> Editar([FromBody] AtualizarPerfilDto dto)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario == null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest(new { erro = "Informe o nome." });

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != usuario.Email)
        {
            var emailEmUso = await _db.Usuarios.AnyAsync(u => u.Email == dto.Email && u.Id != usuarioId);
            if (emailEmUso)
                return BadRequest(new { erro = "Esse e-mail já está em uso por outra conta." });

            usuario.Email = dto.Email;
        }

        usuario.Nome = dto.Nome;
        usuario.Telefone = dto.Telefone;

        await _db.SaveChangesAsync();

        return Ok(new MeuPerfilDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Telefone));
    }

    // LGPD: exclusão dos dados pessoais.
    // Como os agendamentos precisam ser preservados (histórico de transações,
    // e a FK é Restrict), anonimizamos os dados em vez de apagar a linha.
    // Exige a senha atual como confirmação extra, já que é uma ação irreversível.
    [HttpDelete("me")]
    public async Task<IActionResult> ExcluirConta([FromBody] ExcluirContaDto dto)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario == null) return NotFound();

        if (string.IsNullOrEmpty(dto.Senha) || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
            return BadRequest(new { erro = "Senha incorreta." });

        usuario.Nome = "Usuário excluído";
        usuario.Email = $"excluido-{usuario.Id}-{Guid.NewGuid():N}@removido.rzr";
        usuario.Telefone = null;
        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
        usuario.TokenRedefinicaoSenha = null;
        usuario.TokenRedefinicaoExpiraEm = null;

        await _db.SaveChangesAsync();

        return NoContent();
    }
}