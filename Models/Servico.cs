namespace BarberShopApi.Models
{
    public class Servico
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int DuracaoMinutos { get; set; }
        public decimal Preco { get; set; }
        public bool Ativo { get; set; } = true;

        public ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    }
}
