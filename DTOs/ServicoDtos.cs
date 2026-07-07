namespace BarberShopApi.DTOs;

public record ServicoRequestDto(
    string Nome,
    int DuracaoMinutos,
    decimal Preco
);

public record ServicoResponseDto(
    int Id,
    string Nome,
    int DuracaoMinutos,
    decimal Preco,
    bool Ativo
);