namespace BarberShopApi.DTOs;

public record MeuPerfilDto(
    int Id,
    string Nome,
    string Email,
    string? Telefone
);

public record AtualizarPerfilDto(
    string Nome,
    string? Telefone,
    string? Email
);

public record ExcluirContaDto(
    string Senha
);