namespace BarberShopApi.DTOs;

public record CriarAgendamentoDto(
    int BarbeiroId,
    int ServicoId,
    DateTime DataHoraInicio
);

public record HorarioDisponivelDto(
    DateTime Inicio,
    DateTime Fim
);

public record AgendamentoResponseDto(
    int Id,
    string BarbeiroNome,
    string ServicoNome,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    string Status,
    string ClienteNome
);

public record AtualizarStatusDto(
    string Status // "Confirmado" ou "Cancelado"
);