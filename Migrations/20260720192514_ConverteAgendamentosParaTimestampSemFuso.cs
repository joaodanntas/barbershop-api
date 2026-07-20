using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberShopApi.Migrations
{
    /// <inheritdoc />
    public partial class ConverteAgendamentosParaTimestampSemFuso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Agendamentos""
                ALTER COLUMN ""DataHoraInicio"" TYPE timestamp without time zone
                USING ""DataHoraInicio"" AT TIME ZONE 'UTC';
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Agendamentos""
                ALTER COLUMN ""DataHoraFim"" TYPE timestamp without time zone
                USING ""DataHoraFim"" AT TIME ZONE 'UTC';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DataHoraInicio",
                table: "Agendamentos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DataHoraFim",
                table: "Agendamentos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }
    }
}
