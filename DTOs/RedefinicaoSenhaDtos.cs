namespace BarberShopApi.DTOs;

public record EsqueciSenhaDto(string Email);

public record RedefinirSenhaDto(string Token, string NovaSenha);