using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BarberShopApi.Services;

public class EmailService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<EmailService> _logger;

    // Enquanto não configurar domínio próprio no Resend, use o domínio de teste
    private const string RemetentePadrao = "RZR Barber Shop <onboarding@resend.dev>";

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri("https://api.resend.com/") };
        _apiKey = configuration["Resend:ApiKey"]
            ?? throw new InvalidOperationException("Resend:ApiKey não configurada.");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _logger = logger;
    }

    public async Task EnviarAsync(string destinatario, string assunto, string corpoHtml)
    {
        var payload = new
        {
            from = RemetentePadrao,
            to = new[] { destinatario },
            subject = assunto,
            html = corpoHtml
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("emails", content);
            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Falha ao enviar e-mail para {Destinatario}: {Erro}", destinatario, erro);
            }
        }
        catch (Exception ex)
        {
            // Não deixa falha de e-mail quebrar o fluxo principal (ex: criar agendamento)
            _logger.LogError(ex, "Erro ao tentar enviar e-mail para {Destinatario}", destinatario);
        }
    }
}