namespace BarberShopApi.DTOs;

public record LogAdminResponseDto(
    int Id,
    string AdminNome,
    string Acao,
    string Entidade,
    int EntidadeId,
    string? Detalhes,
    DateTime CriadoEm
);