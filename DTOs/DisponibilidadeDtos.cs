namespace BarberShopApi.DTOs;

public record DisponibilidadeRequestDto(
    DayOfWeek DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFim
);

public record DisponibilidadeResponseDto(
    int Id,
    DayOfWeek DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFim
);