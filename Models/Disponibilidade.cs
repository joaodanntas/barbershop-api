namespace BarberShopApi.Models
{
    public class Disponibilidade
    {
        public int Id { get; set; }
        public int BarbeiroId { get; set; }
        public DayOfWeek DiaSemana { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFim { get; set; }
        public TimeOnly? PausaInicio { get; set; }
        public TimeOnly? PausaFim { get; set; }

        public Barbeiro Barbeiro { get; set; } = null!;
    }
}
