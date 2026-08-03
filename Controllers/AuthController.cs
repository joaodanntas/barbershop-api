using BarberShopApi.Data;
using BarberShopApi.DTOs;
using BarberShopApi.Models;
using BarberShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace BarberShopApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly EmailService _emailService;

    private const string FrontendResetUrl = "https://joaodanntas.github.io/barbershop-frontend/redefinir-senha.html";

    public AuthController(AppDbContext db, IConfiguration config, EmailService emailService)
    {
        _db = db;
        _config = config;
        _emailService = emailService;
    }

    [HttpPost("cadastro")]
    [AllowAnonymous]
    public async Task<IActionResult> Cadastro([FromBody] CadastroDto dto)
    {
        // Verifica se email já existe
        if (await _db.Usuarios.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { erro = "Email já cadastrado." });

        var usuario = new Usuario
        {
            Nome = dto.Nome,
            Email = dto.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
            Telefone = dto.Telefone,
            Perfil = "Cliente"
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        var token = GerarToken(usuario);

        return Ok(new AuthResponseDto(token, usuario.Nome, usuario.Email, usuario.Perfil));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
            return Unauthorized(new { erro = "Email ou senha inválidos." });

        var token = GerarToken(usuario);

        return Ok(new AuthResponseDto(token, usuario.Nome, usuario.Email, usuario.Perfil));
    }

    // Solicita redefinição de senha (envia e-mail com token)
    [HttpPost("esqueci-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaDto dto)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);

        // Resposta idêntica exista ou não o e-mail — evita que alguém descubra
        // quais e-mails estão cadastrados testando esse endpoint (enumeration attack)
        if (usuario == null)
            return Ok(new { mensagem = "Se esse e-mail estiver cadastrado, você receberá instruções em instantes." });

        var token = Guid.NewGuid().ToString("N");
        usuario.TokenRedefinicaoSenha = token;
        usuario.TokenRedefinicaoExpiraEm = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        var link = $"{FrontendResetUrl}?token={token}";
        var corpoHtml = $@"
            <h2>Redefinição de senha</h2>
            <p>Olá, {usuario.Nome}!</p>
            <p>Recebemos uma solicitação para redefinir sua senha na RZR Barber Shop.</p>
            <p><a href=""{link}"">Clique aqui para criar uma nova senha</a></p>
            <p>Esse link expira em 1 hora. Se você não solicitou isso, pode ignorar este e-mail com segurança.</p>
        ";
        await _emailService.EnviarAsync(usuario.Email, "Redefinição de senha - RZR Barber Shop", corpoHtml);

        return Ok(new { mensagem = "Se esse e-mail estiver cadastrado, você receberá instruções em instantes." });
    }

    // Confirma a redefinição, usando o token recebido por e-mail
    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDto dto)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.TokenRedefinicaoSenha == dto.Token);

        if (usuario == null || usuario.TokenRedefinicaoExpiraEm == null || usuario.TokenRedefinicaoExpiraEm < DateTime.UtcNow)
            return BadRequest(new { erro = "Link inválido ou expirado. Solicite uma nova redefinição." });

        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
        usuario.TokenRedefinicaoSenha = null;
        usuario.TokenRedefinicaoExpiraEm = null;
        await _db.SaveChangesAsync();

        return Ok(new { mensagem = "Senha redefinida com sucesso! Você já pode fazer login." });
    }

    private string GerarToken(Usuario usuario)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Role, usuario.Perfil)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiracao = DateTime.UtcNow.AddHours(
            int.Parse(_config["Jwt:ExpiracaoHoras"]!));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiracao,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}