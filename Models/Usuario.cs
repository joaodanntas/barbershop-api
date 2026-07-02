namespace BarberShopApi.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string Perfil { get; set; } = "Cliente"; // "Cliente" ou "Admin"
        public string? Telefone { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    }
}