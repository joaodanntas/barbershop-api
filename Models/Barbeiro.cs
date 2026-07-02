namespace BarberShopApi.Models
{
    public class Barbeiro
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public bool Ativo { get; set; } = true;

        public ICollection<Disponibilidade> Disponibilidades { get; set; } = new List<Disponibilidade>();
        public ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    }
}
