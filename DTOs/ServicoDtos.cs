namespace BarberShopApi.DTOs;

public record ServicoRequestDto(
    string Nome,
    int DuracaoMinutos,
    decimal Preco,
    int AntecedenciaMinimaMinutos
);

public record ServicoResponseDto(
    int Id,
    string Nome,
    int DuracaoMinutos,
    decimal Preco,
    bool Ativo,
    int AntecedenciaMinimaMinutos
);