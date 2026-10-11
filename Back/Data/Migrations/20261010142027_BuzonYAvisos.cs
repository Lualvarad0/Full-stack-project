using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class BuzonYAvisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "avisos_cuenta",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    detalle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    dispositivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    enviado_por_correo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_avisos_cuenta", x => x.id);
                    table.ForeignKey(
                        name: "fk_avisos_cuenta_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "buzones_correo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ultimo_correo_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmacion_gmail_codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    confirmacion_gmail_enlace = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    confirmacion_gmail_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buzones_correo", x => x.id);
                    table.ForeignKey(
                        name: "fk_buzones_correo_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "correos_recibidos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    recibido_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    remitente = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    asunto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: true),
                    archivos_xml = table.Column<int>(type: "integer", nullable: false),
                    claves_encontradas = table.Column<int>(type: "integer", nullable: false),
                    importados = table.Column<int>(type: "integer", nullable: false),
                    ingresos = table.Column<int>(type: "integer", nullable: false),
                    detalle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    procesado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    intentos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_correos_recibidos", x => x.id);
                    table.ForeignKey(
                        name: "fk_correos_recibidos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avisos_cuenta_usuario_id_fecha",
                table: "avisos_cuenta",
                columns: new[] { "usuario_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_buzones_correo_codigo",
                table: "buzones_correo",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_buzones_correo_usuario_id",
                table: "buzones_correo",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_correos_recibidos_estado",
                table: "correos_recibidos",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_correos_recibidos_usuario_id_recibido_en",
                table: "correos_recibidos",
                columns: new[] { "usuario_id", "recibido_en" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avisos_cuenta");

            migrationBuilder.DropTable(
                name: "buzones_correo");

            migrationBuilder.DropTable(
                name: "correos_recibidos");
        }
    }
}
