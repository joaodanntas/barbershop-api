namespace BarberShopApi.DTOs;

public record DisponibilidadeRequestDto(
    DayOfWeek DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFim,
    TimeOnly? PausaInicio,
    TimeOnly? PausaFim
);

public record DisponibilidadeResponseDto(
    int Id,
    DayOfWeek DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFim,
    TimeOnly? PausaInicio,
    TimeOnly? PausaFim
);