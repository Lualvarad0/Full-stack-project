using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class TesoreriaYConciliacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "fecha_vencimiento",
                table: "comprobantes",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cuentas_bancarias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    numero_cifrado = table.Column<string>(type: "text", nullable: false),
                    ultimos_digitos = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    saldo_inicial = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fecha_inicial = table.Column<DateOnly>(type: "date", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cuentas_bancarias", x => x.id);
                    table.ForeignKey(
                        name: "fk_cuentas_bancarias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_bancarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    cuenta_bancaria_id = table.Column<int>(type: "integer", nullable: false),
                    id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    concepto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    revisado_sin_factura_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_revision = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimientos_bancarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_movimientos_bancarios_cuentas_bancarias_cuenta_bancaria_id",
                        column: x => x.cuenta_bancaria_id,
                        principalTable: "cuentas_bancarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimientos_bancarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos_comprobantes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    comprobante_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    medio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cuenta_bancaria_id = table.Column<int>(type: "integer", nullable: true),
                    anulado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    motivo_anulacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagos_comprobantes", x => x.id);
                    table.ForeignKey(
                        name: "fk_pagos_comprobantes_comprobantes_comprobante_id",
                        column: x => x.comprobante_id,
                        principalTable: "comprobantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagos_comprobantes_cuenta_bancaria_cuenta_bancaria_id",
                        column: x => x.cuenta_bancaria_id,
                        principalTable: "cuentas_bancarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagos_comprobantes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conciliaciones_bancarias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    movimiento_bancario_id = table.Column<int>(type: "integer", nullable: false),
                    pago_comprobante_id = table.Column<int>(type: "integer", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conciliaciones_bancarias", x => x.id);
                    table.ForeignKey(
                        name: "fk_conciliaciones_bancarias_movimientos_bancarios_movimiento_b",
                        column: x => x.movimiento_bancario_id,
                        principalTable: "movimientos_bancarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conciliaciones_bancarias_pagos_comprobantes_pago_comprobant",
                        column: x => x.pago_comprobante_id,
                        principalTable: "pagos_comprobantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conciliaciones_bancarias_movimiento_bancario_id_pago_compro",
                table: "conciliaciones_bancarias",
                columns: new[] { "movimiento_bancario_id", "pago_comprobante_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conciliaciones_bancarias_pago_comprobante_id",
                table: "conciliaciones_bancarias",
                column: "pago_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_bancarias_usuario_id",
                table: "cuentas_bancarias",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_bancarios_cuenta_bancaria_id_id_externo",
                table: "movimientos_bancarios",
                columns: new[] { "cuenta_bancaria_id", "id_externo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_bancarios_usuario_id",
                table: "movimientos_bancarios",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_comprobantes_comprobante_id",
                table: "pagos_comprobantes",
                column: "comprobante_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_comprobantes_cuenta_bancaria_id",
                table: "pagos_comprobantes",
                column: "cuenta_bancaria_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_comprobantes_usuario_id_fecha",
                table: "pagos_comprobantes",
                columns: new[] { "usuario_id", "fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conciliaciones_bancarias");

            migrationBuilder.DropTable(
                name: "movimientos_bancarios");

            migrationBuilder.DropTable(
                name: "pagos_comprobantes");

            migrationBuilder.DropTable(
                name: "cuentas_bancarias");

            migrationBuilder.DropColumn(
                name: "fecha_vencimiento",
                table: "comprobantes");
        }
    }
}
