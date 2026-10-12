using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class RetencionesYDocumentosComplementarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "detalle_sustento_id",
                table: "facturas_emitidas_detalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_sustento",
                table: "facturas_emitidas",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "comprobante_sustento_id",
                table: "facturas_emitidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "documento_sustento_id",
                table: "facturas_emitidas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "fecha_documento_sustento",
                table: "facturas_emitidas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo",
                table: "facturas_emitidas",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "numero_documento_sustento",
                table: "facturas_emitidas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo_documento",
                table: "facturas_emitidas",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "01");

            migrationBuilder.AddColumn<bool>(
                name: "agente_retencion",
                table: "datos_emisor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "retenciones_emitidas_detalle",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    factura_emitida_id = table.Column<int>(type: "integer", nullable: false),
                    codigo_impuesto = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    codigo_retencion = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retenciones_emitidas_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_retenciones_emitidas_detalle_facturas_emitidas_factura_emit",
                        column: x => x.factura_emitida_id,
                        principalTable: "facturas_emitidas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "retenciones_sri",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    direccion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    clave_acceso = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: false),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    periodo_fiscal = table.Column<DateOnly>(type: "date", nullable: false),
                    ruc_emisor = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    razon_social_emisor = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    identificacion_retenido = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    razon_social_retenido = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    xml = table.Column<string>(type: "text", nullable: false),
                    numero_autorizacion = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: false),
                    fecha_autorizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revisado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retenciones_sri", x => x.id);
                    table.ForeignKey(
                        name: "fk_retenciones_sri_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retenciones_sri_detalle",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    retencion_sri_id = table.Column<int>(type: "integer", nullable: false),
                    codigo_impuesto = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    codigo_retencion = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    base_imponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    documento_sustento = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: true),
                    comprobante_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retenciones_sri_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_retenciones_sri_detalle_comprobantes_comprobante_id",
                        column: x => x.comprobante_id,
                        principalTable: "comprobantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_retenciones_sri_detalle_retenciones_sri_retencion_sri_id",
                        column: x => x.retencion_sri_id,
                        principalTable: "retenciones_sri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_facturas_emitidas_comprobante_sustento_id",
                table: "facturas_emitidas",
                column: "comprobante_sustento_id");

            migrationBuilder.CreateIndex(
                name: "ix_facturas_emitidas_documento_sustento_id",
                table: "facturas_emitidas",
                column: "documento_sustento_id");

            migrationBuilder.CreateIndex(
                name: "ix_retenciones_emitidas_detalle_factura_emitida_id",
                table: "retenciones_emitidas_detalle",
                column: "factura_emitida_id");

            migrationBuilder.CreateIndex(
                name: "ix_retenciones_sri_usuario_id_clave_acceso",
                table: "retenciones_sri",
                columns: new[] { "usuario_id", "clave_acceso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_retenciones_sri_detalle_comprobante_id",
                table: "retenciones_sri_detalle",
                column: "comprobante_id");

            migrationBuilder.CreateIndex(
                name: "ix_retenciones_sri_detalle_retencion_sri_id",
                table: "retenciones_sri_detalle",
                column: "retencion_sri_id");

            migrationBuilder.AddForeignKey(
                name: "fk_facturas_emitidas_comprobantes_comprobante_sustento_id",
                table: "facturas_emitidas",
                column: "comprobante_sustento_id",
                principalTable: "comprobantes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_facturas_emitidas_facturas_emitidas_documento_sustento_id",
                table: "facturas_emitidas",
                column: "documento_sustento_id",
                principalTable: "facturas_emitidas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_facturas_emitidas_comprobantes_comprobante_sustento_id",
                table: "facturas_emitidas");

            migrationBuilder.DropForeignKey(
                name: "fk_facturas_emitidas_facturas_emitidas_documento_sustento_id",
                table: "facturas_emitidas");

            migrationBuilder.DropTable(
                name: "retenciones_emitidas_detalle");

            migrationBuilder.DropTable(
                name: "retenciones_sri_detalle");

            migrationBuilder.DropTable(
                name: "retenciones_sri");

            migrationBuilder.DropIndex(
                name: "ix_facturas_emitidas_comprobante_sustento_id",
                table: "facturas_emitidas");

            migrationBuilder.DropIndex(
                name: "ix_facturas_emitidas_documento_sustento_id",
                table: "facturas_emitidas");

            migrationBuilder.DropColumn(
                name: "detalle_sustento_id",
                table: "facturas_emitidas_detalle");

            migrationBuilder.DropColumn(
                name: "codigo_sustento",
                table: "facturas_emitidas");

            migrationBuilder.DropColumn(
                name: "comprobante_sustento_id",
                table: "facturas_emitidas");

            migrationBuilder.DropColumn(
                name: "documento_sustento_id",
                table: "facturas_emitidas");

            migrationBuilder.DropColumn(
                name: "fecha_documento_sustento",
                table: "facturas_emitidas");

            migrationBuilder.DropColumn(
                name: "motivo",
                table: "facturas_emitidas");

            migrationBuilder.DropColumn(
                name: "numero_documento_sustento",
                table: "facturas_emitidas");

            migrationBuilder.DropColumn(
                name: "tipo_documento",
                table: "facturas_emitidas");

            migrationBuilder.DropColumn(
                name: "agente_retencion",
                table: "datos_emisor");
        }
    }
}
