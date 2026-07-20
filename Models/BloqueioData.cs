namespace BarberShopApi.Models
{
    public class BloqueioData
    {
        public int Id { get; set; }
        public int? BarbeiroId { get; set; } // null = bloqueio global (feriado da barbearia)
        public DateOnly Data { get; set; }
        public string? Motivo { get; set; }

        public Barbeiro? Barbeiro { get; set; }
    }
}
