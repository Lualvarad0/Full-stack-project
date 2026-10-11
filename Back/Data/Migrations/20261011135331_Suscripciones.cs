using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class Suscripciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "suscripciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    periodicidad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    kushki_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    iva = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    creada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    proximo_cobro = table.Column<DateOnly>(type: "date", nullable: false),
                    ultimo_pago_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelada_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    marca_tarjeta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ultimos_digitos = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suscripciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_suscripciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pagos_suscripcion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    suscripcion_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    aprobado = table.Column<bool>(type: "boolean", nullable: false),
                    ticket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    detalle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagos_suscripcion", x => x.id);
                    table.ForeignKey(
                        name: "fk_pagos_suscripcion_suscripciones_suscripcion_id",
                        column: x => x.suscripcion_id,
                        principalTable: "suscripciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pagos_suscripcion_suscripcion_id",
                table: "pagos_suscripcion",
                column: "suscripcion_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_suscripcion_ticket",
                table: "pagos_suscripcion",
                column: "ticket",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suscripciones_kushki_id",
                table: "suscripciones",
                column: "kushki_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suscripciones_usuario_id_estado",
                table: "suscripciones",
                columns: new[] { "usuario_id", "estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pagos_suscripcion");

            migrationBuilder.DropTable(
                name: "suscripciones");
        }
    }
}
