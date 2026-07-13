using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberShopApi.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaIndiceUnicoDisponibilidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Disponibilidades_BarbeiroId",
                table: "Disponibilidades");

            migrationBuilder.CreateIndex(
                name: "IX_Disponibilidades_BarbeiroId_DiaSemana",
                table: "Disponibilidades",
                columns: new[] { "BarbeiroId", "DiaSemana" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Disponibilidades_BarbeiroId_DiaSemana",
                table: "Disponibilidades");

            migrationBuilder.CreateIndex(
                name: "IX_Disponibilidades_BarbeiroId",
                table: "Disponibilidades",
                column: "BarbeiroId");
        }
    }
}
