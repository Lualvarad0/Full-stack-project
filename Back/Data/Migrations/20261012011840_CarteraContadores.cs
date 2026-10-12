using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class CarteraContadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vinculos_contador",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    propietario_id = table.Column<int>(type: "integer", nullable: false),
                    contador_id = table.Column<int>(type: "integer", nullable: false),
                    solicitado_por_id = table.Column<int>(type: "integer", nullable: false),
                    permiso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    solicitado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    respondido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vinculos_contador", x => x.id);
                    table.ForeignKey(
                        name: "fk_vinculos_contador_usuarios_contador_id",
                        column: x => x.contador_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vinculos_contador_usuarios_propietario_id",
                        column: x => x.propietario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vinculos_contador_contador_id",
                table: "vinculos_contador",
                column: "contador_id");

            migrationBuilder.CreateIndex(
                name: "ix_vinculos_contador_propietario_id_contador_id",
                table: "vinculos_contador",
                columns: new[] { "propietario_id", "contador_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vinculos_contador");
        }
    }
}
