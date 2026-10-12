using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClasificacionPartidasYCierre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "categoria",
                table: "comprobantes_detalle",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "categoria_ingreso",
                table: "comprobantes_detalle",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "finalidad",
                table: "comprobantes_detalle",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SinDefinir");

            migrationBuilder.AddColumn<decimal>(
                name: "iva",
                table: "comprobantes_detalle",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "revisado_en",
                table: "comprobantes_detalle",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tarifa_iva",
                table: "comprobantes_detalle",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tratamiento_iva",
                table: "comprobantes_detalle",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pendiente");

            migrationBuilder.AddColumn<string>(
                name: "finalidad",
                table: "comprobantes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SinDefinir");

            migrationBuilder.AddColumn<DateTime>(
                name: "revisado_en",
                table: "comprobantes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tratamiento_iva",
                table: "comprobantes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pendiente");

            migrationBuilder.CreateTable(
                name: "auditoria_contable",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entidad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    detalle = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria_contable", x => x.id);
                    table.ForeignKey(
                        name: "fk_auditoria_contable_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "periodos_contables",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    mes = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cerrado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cerrado_por_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_periodos_contables", x => x.id);
                    table.ForeignKey(
                        name: "fk_periodos_contables_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_contable_usuario_id_fecha",
                table: "auditoria_contable",
                columns: new[] { "usuario_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_periodos_contables_usuario_id_anio_mes",
                table: "periodos_contables",
                columns: new[] { "usuario_id", "anio", "mes" },
                unique: true);
            // Conservar elecciones manuales anteriores; ninguna sugerencia se confirma aquí.
            migrationBuilder.Sql("UPDATE comprobantes SET finalidad = CASE WHEN categoria = 'NoDeducible' THEN 'NoAplicable' ELSE 'Personal' END, revisado_en = fecha_importacion WHERE direccion = 'Recibido' AND categoria <> 'SinCategoria'");
            migrationBuilder.Sql("UPDATE comprobantes SET finalidad = 'Negocio', revisado_en = fecha_importacion WHERE direccion = 'Emitido' AND categoria_ingreso IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria_contable");

            migrationBuilder.DropTable(
                name: "periodos_contables");

            migrationBuilder.DropColumn(
                name: "categoria",
                table: "comprobantes_detalle");

            migrationBuilder.DropColumn(
                name: "categoria_ingreso",
                table: "comprobantes_detalle");

            migrationBuilder.DropColumn(
                name: "finalidad",
                table: "comprobantes_detalle");

            migrationBuilder.DropColumn(
                name: "iva",
                table: "comprobantes_detalle");

            migrationBuilder.DropColumn(
                name: "revisado_en",
                table: "comprobantes_detalle");

            migrationBuilder.DropColumn(
                name: "tarifa_iva",
                table: "comprobantes_detalle");

            migrationBuilder.DropColumn(
                name: "tratamiento_iva",
                table: "comprobantes_detalle");

            migrationBuilder.DropColumn(
                name: "finalidad",
                table: "comprobantes");

            migrationBuilder.DropColumn(
                name: "revisado_en",
                table: "comprobantes");

            migrationBuilder.DropColumn(
                name: "tratamiento_iva",
                table: "comprobantes");
        }
    }
}
