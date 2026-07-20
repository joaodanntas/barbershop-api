using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BarberShopApi.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaBloqueioData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloqueiosData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BarbeiroId = table.Column<int>(type: "integer", nullable: true),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueiosData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloqueiosData_Barbeiros_BarbeiroId",
                        column: x => x.BarbeiroId,
                        principalTable: "Barbeiros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloqueiosData_BarbeiroId_Data",
                table: "BloqueiosData",
                columns: new[] { "BarbeiroId", "Data" },
                unique: true,
                filter: "\"BarbeiroId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueiosData_Data",
                table: "BloqueiosData",
                column: "Data",
                unique: true,
                filter: "\"BarbeiroId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloqueiosData");
        }
    }
}
