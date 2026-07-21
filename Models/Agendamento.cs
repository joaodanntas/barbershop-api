namespace BarberShopApi.Models
{
    public class Agendamento
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int BarbeiroId { get; set; }
        public int ServicoId { get; set; }
        public DateTime DataHoraInicio { get; set; }
        public DateTime DataHoraFim { get; set; }
        public string Status { get; set; } = "Pendente"; // Pendente, Confirmado, Cancelado
        public string? TokenCancelamento { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public bool LembreteEnviado { get; set; } = false;

        public Usuario Usuario { get; set; } = null!;
        public Barbeiro Barbeiro { get; set; } = null!;
        public Servico Servico { get; set; } = null!;
    }
}
