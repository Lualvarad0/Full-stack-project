using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class TarjetasYCierreDeMes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "comprobante_id",
                table: "cuentas_personales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "respaldo",
                table: "cuentas_personales",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tarjeta_credito_id",
                table: "cuentas_personales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "trasladada_a_cuenta_id",
                table: "cuentas_personales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "trasladada_desde_id",
                table: "cuentas_personales",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tarjetas_credito",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    alias = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    bin = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    ultimos4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    marca = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    banco = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    verificada = table.Column<bool>(type: "boolean", nullable: false),
                    creada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tarjetas_credito", x => x.id);
                    table.ForeignKey(
                        name: "fk_tarjetas_credito_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "estados_cuenta_tarjeta",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    tarjeta_credito_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_corte = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_maxima_pago = table.Column<DateOnly>(type: "date", nullable: true),
                    saldo_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    pago_minimo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cuenta_personal_id = table.Column<int>(type: "integer", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estados_cuenta_tarjeta", x => x.id);
                    table.ForeignKey(
                        name: "fk_estados_cuenta_tarjeta_cuentas_personales_cuenta_personal_id",
                        column: x => x.cuenta_personal_id,
                        principalTable: "cuentas_personales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_estados_cuenta_tarjeta_tarjetas_credito_tarjeta_credito_id",
                        column: x => x.tarjeta_credito_id,
                        principalTable: "tarjetas_credito",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_estados_cuenta_tarjeta_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_personales_comprobante_id",
                table: "cuentas_personales",
                column: "comprobante_id");

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_personales_tarjeta_credito_id",
                table: "cuentas_personales",
                column: "tarjeta_credito_id");

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_personales_trasladada_a_cuenta_id",
                table: "cuentas_personales",
                column: "trasladada_a_cuenta_id");

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_personales_trasladada_desde_id",
                table: "cuentas_personales",
                column: "trasladada_desde_id");

            migrationBuilder.CreateIndex(
                name: "ix_estados_cuenta_tarjeta_cuenta_personal_id",
                table: "estados_cuenta_tarjeta",
                column: "cuenta_personal_id");

            migrationBuilder.CreateIndex(
                name: "ix_estados_cuenta_tarjeta_tarjeta_credito_id",
                table: "estados_cuenta_tarjeta",
                column: "tarjeta_credito_id");

            migrationBuilder.CreateIndex(
                name: "ix_estados_cuenta_tarjeta_usuario_id_estado",
                table: "estados_cuenta_tarjeta",
                columns: new[] { "usuario_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_tarjetas_credito_usuario_id_bin_ultimos4",
                table: "tarjetas_credito",
                columns: new[] { "usuario_id", "bin", "ultimos4" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_cuentas_personales_comprobantes_comprobante_id",
                table: "cuentas_personales",
                column: "comprobante_id",
                principalTable: "comprobantes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_cuentas_personales_cuentas_personales_trasladada_a_cuenta_id",
                table: "cuentas_personales",
                column: "trasladada_a_cuenta_id",
                principalTable: "cuentas_personales",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_cuentas_personales_cuentas_personales_trasladada_desde_id",
                table: "cuentas_personales",
                column: "trasladada_desde_id",
                principalTable: "cuentas_personales",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_cuentas_personales_tarjetas_credito_tarjeta_credito_id",
                table: "cuentas_personales",
                column: "tarjeta_credito_id",
                principalTable: "tarjetas_credito",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cuentas_personales_comprobantes_comprobante_id",
                table: "cuentas_personales");

            migrationBuilder.DropForeignKey(
                name: "fk_cuentas_personales_cuentas_personales_trasladada_a_cuenta_id",
                table: "cuentas_personales");

            migrationBuilder.DropForeignKey(
                name: "fk_cuentas_personales_cuentas_personales_trasladada_desde_id",
                table: "cuentas_personales");

            migrationBuilder.DropForeignKey(
                name: "fk_cuentas_personales_tarjetas_credito_tarjeta_credito_id",
                table: "cuentas_personales");

            migrationBuilder.DropTable(
                name: "estados_cuenta_tarjeta");

            migrationBuilder.DropTable(
                name: "tarjetas_credito");

            migrationBuilder.DropIndex(
                name: "ix_cuentas_personales_comprobante_id",
                table: "cuentas_personales");

            migrationBuilder.DropIndex(
                name: "ix_cuentas_personales_tarjeta_credito_id",
                table: "cuentas_personales");

            migrationBuilder.DropIndex(
                name: "ix_cuentas_personales_trasladada_a_cuenta_id",
                table: "cuentas_personales");

            migrationBuilder.DropIndex(
                name: "ix_cuentas_personales_trasladada_desde_id",
                table: "cuentas_personales");

            migrationBuilder.DropColumn(
                name: "comprobante_id",
                table: "cuentas_personales");

            migrationBuilder.DropColumn(
                name: "respaldo",
                table: "cuentas_personales");

            migrationBuilder.DropColumn(
                name: "tarjeta_credito_id",
                table: "cuentas_personales");

            migrationBuilder.DropColumn(
                name: "trasladada_a_cuenta_id",
                table: "cuentas_personales");

            migrationBuilder.DropColumn(
                name: "trasladada_desde_id",
                table: "cuentas_personales");
        }
    }
}
