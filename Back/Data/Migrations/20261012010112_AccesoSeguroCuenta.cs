using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccesoSeguroCuenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "correo_verificado",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "version_seguridad",
                table: "usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "correos_acceso_pendientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    contenido_cifrado = table.Column<string>(type: "text", nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    proximo_intento_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    enviado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_correos_acceso_pendientes", x => x.id);
                    table.ForeignKey(
                        name: "fk_correos_acceso_pendientes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sesiones_cuenta",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    creada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revocada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dispositivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sesiones_cuenta", x => x.id);
                    table.ForeignKey(
                        name: "fk_sesiones_cuenta_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tokens_cuenta",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    proposito = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tokens_cuenta", x => x.id);
                    table.ForeignKey(
                        name: "fk_tokens_cuenta_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_correos_acceso_pendientes_enviado_en_proximo_intento_en",
                table: "correos_acceso_pendientes",
                columns: new[] { "enviado_en", "proximo_intento_en" });

            migrationBuilder.CreateIndex(
                name: "ix_correos_acceso_pendientes_usuario_id",
                table: "correos_acceso_pendientes",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_sesiones_cuenta_usuario_id_expira_en",
                table: "sesiones_cuenta",
                columns: new[] { "usuario_id", "expira_en" });

            migrationBuilder.CreateIndex(
                name: "ix_tokens_cuenta_hash",
                table: "tokens_cuenta",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tokens_cuenta_usuario_id_proposito_creado_en",
                table: "tokens_cuenta",
                columns: new[] { "usuario_id", "proposito", "creado_en" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "correos_acceso_pendientes");

            migrationBuilder.DropTable(
                name: "sesiones_cuenta");

            migrationBuilder.DropTable(
                name: "tokens_cuenta");

            migrationBuilder.DropColumn(
                name: "correo_verificado",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "version_seguridad",
                table: "usuarios");
        }
    }
}
