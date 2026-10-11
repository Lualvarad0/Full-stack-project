using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegistroYPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "apellido",
                table: "usuarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "fecha_nacimiento",
                table: "usuarios",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plan",
                table: "usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Personal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "apellido",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "fecha_nacimiento",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "plan",
                table: "usuarios");
        }
    }
}
