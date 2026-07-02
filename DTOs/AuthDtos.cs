namespace BarberShopApi.DTOs;

public record CadastroDto(
    string Nome,
    string Email,
    string Senha,
    string? Telefone
);

public record LoginDto(
    string Email,
    string Senha
);

public record AuthResponseDto(
    string Token,
    string Nome,
    string Email,
    string Perfil
);