using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class PerfilYConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "avisar_ingresos_por_correo",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ciudad",
                table: "usuarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "direccion",
                table: "usuarios",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mes_al_entrar",
                table: "usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Actual");

            migrationBuilder.AddColumn<string>(
                name: "ocupacion",
                table: "usuarios",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pagina_inicio",
                table: "usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Resumen");

            migrationBuilder.AddColumn<string>(
                name: "telefono",
                table: "usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avisar_ingresos_por_correo",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "ciudad",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "direccion",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "mes_al_entrar",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "ocupacion",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "pagina_inicio",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "telefono",
                table: "usuarios");
        }
    }
}
