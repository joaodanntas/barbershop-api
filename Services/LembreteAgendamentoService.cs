using BarberShopApi.Data;
using BarberShopApi.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BarberShopApi.Services;

public class LembreteAgendamentoService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LembreteAgendamentoService> _logger;
    private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromMinutes(10);
    private const int AntecedenciaLembreteHoras = 2;

    public LembreteAgendamentoService(IServiceScopeFactory scopeFactory, ILogger<LembreteAgendamentoService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarEEnviarLembretes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar lembretes de agendamento.");
            }

            await Task.Delay(IntervaloVerificacao, stoppingToken);
        }
    }

    private async Task VerificarEEnviarLembretes()
    {
        // BackgroundService é Singleton; precisa criar um escopo novo pra usar serviços Scoped (DbContext, EmailService)
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

        var agora = TimeHelper.AgoraBrasil();
        var limite = agora.AddHours(AntecedenciaLembreteHoras);

        var agendamentos = await db.Agendamentos
            .Include(a => a.Usuario)
            .Include(a => a.Servico)
            .Include(a => a.Barbeiro)
            .Where(a => a.Status == "Confirmado"
                     && !a.LembreteEnviado
                     && a.DataHoraInicio >= agora
                     && a.DataHoraInicio <= limite)
            .ToListAsync();

        foreach (var agendamento in agendamentos)
        {
            var dataFormatada = agendamento.DataHoraInicio.ToString("dd/MM/yyyy 'às' HH:mm");
            var corpoHtml = $@"
                <h2>Seu horário está chegando!</h2>
                <p>Olá, {agendamento.Usuario.Nome}!</p>
                <p>Passando para lembrar do seu agendamento:</p>
                <ul>
                    <li><strong>Serviço:</strong> {agendamento.Servico.Nome}</li>
                    <li><strong>Barbeiro:</strong> {agendamento.Barbeiro.Nome}</li>
                    <li><strong>Data/Hora:</strong> {dataFormatada}</li>
                </ul>
                <p>Te esperamos na RZR Barber Shop!</p>
            ";

            await emailService.EnviarAsync(agendamento.Usuario.Email, "Lembrete: seu horário está chegando - RZR Barber Shop", corpoHtml);

            agendamento.LembreteEnviado = true;
        }

        if (agendamentos.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }
}