using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class LibrosContables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "asientos_contables",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    concepto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    comprobante_id = table.Column<int>(type: "integer", nullable: true),
                    pago_comprobante_id = table.Column<int>(type: "integer", nullable: true),
                    contabilizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asientos_contables", x => x.id);
                    table.ForeignKey(
                        name: "fk_asientos_contables_comprobantes_comprobante_id",
                        column: x => x.comprobante_id,
                        principalTable: "comprobantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_asientos_contables_pago_comprobante_pago_comprobante_id",
                        column: x => x.pago_comprobante_id,
                        principalTable: "pagos_comprobantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_asientos_contables_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_contables",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cuentas_contables", x => x.id);
                    table.ForeignKey(
                        name: "fk_cuentas_contables_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lineas_asientos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asiento_contable_id = table.Column<int>(type: "integer", nullable: false),
                    cuenta_contable_id = table.Column<int>(type: "integer", nullable: false),
                    debe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    haber = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lineas_asientos", x => x.id);
                    table.ForeignKey(
                        name: "fk_lineas_asientos_asientos_contables_asiento_contable_id",
                        column: x => x.asiento_contable_id,
                        principalTable: "asientos_contables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lineas_asientos_cuentas_contables_cuenta_contable_id",
                        column: x => x.cuenta_contable_id,
                        principalTable: "cuentas_contables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asientos_contables_comprobante_id",
                table: "asientos_contables",
                column: "comprobante_id");

            migrationBuilder.CreateIndex(
                name: "ix_asientos_contables_pago_comprobante_id",
                table: "asientos_contables",
                column: "pago_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "ix_asientos_contables_usuario_id_comprobante_id",
                table: "asientos_contables",
                columns: new[] { "usuario_id", "comprobante_id" },
                unique: true,
                filter: "\"comprobante_id\" IS NOT NULL AND \"estado\" <> 'Anulado'");

            migrationBuilder.CreateIndex(
                name: "ix_asientos_contables_usuario_id_fecha",
                table: "asientos_contables",
                columns: new[] { "usuario_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_asientos_contables_usuario_id_pago_comprobante_id",
                table: "asientos_contables",
                columns: new[] { "usuario_id", "pago_comprobante_id" },
                unique: true,
                filter: "\"pago_comprobante_id\" IS NOT NULL AND \"estado\" <> 'Anulado'");

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_contables_usuario_id_codigo",
                table: "cuentas_contables",
                columns: new[] { "usuario_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lineas_asientos_asiento_contable_id",
                table: "lineas_asientos",
                column: "asiento_contable_id");

            migrationBuilder.CreateIndex(
                name: "ix_lineas_asientos_cuenta_contable_id",
                table: "lineas_asientos",
                column: "cuenta_contable_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lineas_asientos");

            migrationBuilder.DropTable(
                name: "asientos_contables");

            migrationBuilder.DropTable(
                name: "cuentas_contables");
        }
    }
}
