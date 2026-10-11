using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class Facturacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "datos_emisor",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    ambiente = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    establecimiento = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    punto_emision = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    siguiente_secuencial = table.Column<int>(type: "integer", nullable: false),
                    nombre_comercial = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    direccion_matriz = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    direccion_establecimiento = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    obligado_contabilidad = table.Column<bool>(type: "boolean", nullable: false),
                    contribuyente_especial = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_datos_emisor", x => x.id);
                    table.ForeignKey(
                        name: "fk_datos_emisor_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "facturas_emitidas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    ambiente = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    clave_acceso = table.Column<string>(type: "character(49)", fixedLength: true, maxLength: 49, nullable: false),
                    establecimiento = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    punto_emision = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    secuencial = table.Column<int>(type: "integer", nullable: false),
                    fecha_emision = table.Column<DateOnly>(type: "date", nullable: false),
                    creada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo_identificacion_comprador = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    identificacion_comprador = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    razon_social_comprador = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    correo_comprador = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    direccion_comprador = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    forma_pago = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    iva = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    mensajes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    numero_autorizacion = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: true),
                    fecha_autorizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xml_firmado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facturas_emitidas", x => x.id);
                    table.ForeignKey(
                        name: "fk_facturas_emitidas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "firmas_electronicas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    archivo_cifrado = table.Column<byte[]>(type: "bytea", nullable: false),
                    clave_cifrada = table.Column<byte[]>(type: "bytea", nullable: false),
                    titular = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    identificacion = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    emisor = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    numero_serie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    valida_desde = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valida_hasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subida_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_firmas_electronicas", x => x.id);
                    table.ForeignKey(
                        name: "fk_firmas_electronicas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "facturas_emitidas_detalle",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    factura_emitida_id = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tarifa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    iva = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facturas_emitidas_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_facturas_emitidas_detalle_facturas_emitidas_factura_emitida",
                        column: x => x.factura_emitida_id,
                        principalTable: "facturas_emitidas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_datos_emisor_usuario_id",
                table: "datos_emisor",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_facturas_emitidas_clave_acceso",
                table: "facturas_emitidas",
                column: "clave_acceso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_facturas_emitidas_usuario_id_ambiente_establecimiento_punto",
                table: "facturas_emitidas",
                columns: new[] { "usuario_id", "ambiente", "establecimiento", "punto_emision", "secuencial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_facturas_emitidas_usuario_id_creada_en",
                table: "facturas_emitidas",
                columns: new[] { "usuario_id", "creada_en" });

            migrationBuilder.CreateIndex(
                name: "ix_facturas_emitidas_detalle_factura_emitida_id",
                table: "facturas_emitidas_detalle",
                column: "factura_emitida_id");

            migrationBuilder.CreateIndex(
                name: "ix_firmas_electronicas_usuario_id",
                table: "firmas_electronicas",
                column: "usuario_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "datos_emisor");

            migrationBuilder.DropTable(
                name: "facturas_emitidas_detalle");

            migrationBuilder.DropTable(
                name: "firmas_electronicas");

            migrationBuilder.DropTable(
                name: "facturas_emitidas");
        }
    }
}
