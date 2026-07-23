using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberShopApi.Migrations
{
    /// <inheritdoc />
    public partial class CorrigeIndiceAgendamentoParaExcluirCancelados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agendamentos_BarbeiroId_DataHoraInicio",
                table: "Agendamentos");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_BarbeiroId_DataHoraInicio",
                table: "Agendamentos",
                columns: new[] { "BarbeiroId", "DataHoraInicio" },
                unique: true,
                filter: "\"Status\" <> 'Cancelado'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agendamentos_BarbeiroId_DataHoraInicio",
                table: "Agendamentos");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_BarbeiroId_DataHoraInicio",
                table: "Agendamentos",
                columns: new[] { "BarbeiroId", "DataHoraInicio" },
                unique: true);
        }
    }
}
