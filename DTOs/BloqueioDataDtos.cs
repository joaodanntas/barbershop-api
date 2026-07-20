namespace BarberShopApi.DTOs
{
    public record BloqueioDataRequestDto(
        int? BarbeiroId,
        DateOnly Data,
        string? Motivo
    );

    public record BloqueioDataResponseDto(
        int Id,
        int? BarbeiroId,
        string? BarbeiroNome,
        DateOnly Data,
        string? Motivo
    );
}
