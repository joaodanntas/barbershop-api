using BarberShopApi.Data;
using BarberShopApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberShopApi.Controllers;

[ApiController]
[Route("api/logsadmin")]
[Authorize(Roles = "Admin")]
public class LogsAdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public LogsAdminController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20)
    {
        if (pagina < 1) pagina = 1;
        if (tamanhoPagina < 1 || tamanhoPagina > 100) tamanhoPagina = 20;

        var query = _db.LogsAdmin.OrderByDescending(l => l.CriadoEm);

        var totalItens = await query.CountAsync();
        var totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

        var logs = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(l => new LogAdminResponseDto(
                l.Id, l.AdminNome, l.Acao, l.Entidade, l.EntidadeId, l.Detalhes, l.CriadoEm))
            .ToListAsync();

        return Ok(new PaginaDto<LogAdminResponseDto>(logs, pagina, totalPaginas, totalItens));
    }
}