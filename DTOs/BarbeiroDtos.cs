namespace BarberShopApi.DTOs;

public record BarbeiroRequestDto(
    string Nome,
    string? Telefone
);

public record BarbeiroResponseDto(
    int Id,
    string Nome,
    string? Telefone,
    bool Ativo
);