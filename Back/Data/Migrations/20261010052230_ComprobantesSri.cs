using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class ComprobantesSri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comprobantes_recibidos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    clave_acceso = table.Column<string>(type: "character(49)", fixedLength: true, maxLength: 49, nullable: false),
                    tipo_comprobante = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    numero_comprobante = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    numero_autorizacion = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: false),
                    fecha_autorizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    ruc_emisor = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    razon_social_emisor = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    nombre_comercial = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    identificacion_comprador = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    razon_social_comprador = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    base_iva0 = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    base_iva_gravada = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    iva = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    otros_impuestos = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    propina = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    documento_modificado = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    categoria = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_importacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comprobantes_recibidos", x => x.id);
                    table.ForeignKey(
                        name: "fk_comprobantes_recibidos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proveedores_categoria",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    ruc = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    categoria = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proveedores_categoria", x => x.id);
                    table.ForeignKey(
                        name: "fk_proveedores_categoria_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comprobantes_detalle",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comprobante_recibido_id = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comprobantes_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_comprobantes_detalle_comprobantes_recibidos_comprobante_rec",
                        column: x => x.comprobante_recibido_id,
                        principalTable: "comprobantes_recibidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comprobantes_xml",
                columns: table => new
                {
                    comprobante_recibido_id = table.Column<int>(type: "integer", nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comprobantes_xml", x => x.comprobante_recibido_id);
                    table.ForeignKey(
                        name: "fk_comprobantes_xml_comprobantes_recibidos_comprobante_recibid",
                        column: x => x.comprobante_recibido_id,
                        principalTable: "comprobantes_recibidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_comprobantes_detalle_comprobante_recibido_id",
                table: "comprobantes_detalle",
                column: "comprobante_recibido_id");

            migrationBuilder.CreateIndex(
                name: "ix_comprobantes_recibidos_usuario_id_clave_acceso",
                table: "comprobantes_recibidos",
                columns: new[] { "usuario_id", "clave_acceso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_comprobantes_recibidos_usuario_id_fecha_emision",
                table: "comprobantes_recibidos",
                columns: new[] { "usuario_id", "fecha_emision" });

            migrationBuilder.CreateIndex(
                name: "ix_comprobantes_recibidos_usuario_id_ruc_emisor",
                table: "comprobantes_recibidos",
                columns: new[] { "usuario_id", "ruc_emisor" });

            migrationBuilder.CreateIndex(
                name: "ix_proveedores_categoria_usuario_id_ruc",
                table: "proveedores_categoria",
                columns: new[] { "usuario_id", "ruc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comprobantes_detalle");

            migrationBuilder.DropTable(
                name: "comprobantes_xml");

            migrationBuilder.DropTable(
                name: "proveedores_categoria");

            migrationBuilder.DropTable(
                name: "comprobantes_recibidos");
        }
    }
}
