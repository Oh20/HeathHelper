using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerConsumer.Migrations
{
    /// <inheritdoc />
    public partial class AtualizacaoDeCampos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ValorConsulta",
                table: "Agendas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Medico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CRM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Especialidade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CPF = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Senha = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medico", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agendas_MedicoId",
                table: "Agendas",
                column: "MedicoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agendas_Medico_MedicoId",
                table: "Agendas",
                column: "MedicoId",
                principalTable: "Medico",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendas_Medico_MedicoId",
                table: "Agendas");

            migrationBuilder.DropTable(
                name: "Medico");

            migrationBuilder.DropIndex(
                name: "IX_Agendas_MedicoId",
                table: "Agendas");

            migrationBuilder.DropColumn(
                name: "ValorConsulta",
                table: "Agendas");
        }
    }
}
