using BarberShopApi.Data;
using BarberShopApi.Models;

namespace BarberShopApi.Services;

public class LogAdminService
{
    private readonly AppDbContext _db;

    public LogAdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RegistrarAsync(int adminId, string adminNome, string acao, string entidade, int entidadeId, string? detalhes = null)
    {
        var log = new LogAdmin
        {
            AdminId = adminId,
            AdminNome = adminNome,
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Detalhes = detalhes
        };

        _db.LogsAdmin.Add(log);
        await _db.SaveChangesAsync();
    }
}