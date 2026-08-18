namespace BarberShopApi.DTOs;

public record BarbeiroRequestDto(
    string Nome,
    string? Telefone,
    string? FotoBase64
);

public record BarbeiroResponseDto(
    int Id,
    string Nome,
    string? Telefone,
    bool Ativo,
    string? FotoBase64
);