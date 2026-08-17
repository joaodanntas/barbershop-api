namespace BarberShopApi.Models
{
    public class LogAdmin
    {
        public int Id { get; set; }
        public int AdminId { get; set; }
        public string AdminNome { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string Entidade { get; set; } = string.Empty;
        public int EntidadeId { get; set; }
        public string? Detalhes { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}