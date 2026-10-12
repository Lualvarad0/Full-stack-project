using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreparacionTributaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cargas_familiares",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parentesco = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha_nacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    ingresos_gravados = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discapacidad = table.Column<bool>(type: "boolean", nullable: false),
                    enfermedad_catastrofica = table.Column<bool>(type: "boolean", nullable: false),
                    dependencia_confirmada = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cargas_familiares", x => x.id);
                    table.ForeignKey(
                        name: "fk_cargas_familiares_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "perfil_obligaciones",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    iva = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    retenciones = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ats = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    renta = table.Column<bool>(type: "boolean", nullable: false),
                    gastos_personales = table.Column<bool>(type: "boolean", nullable: false),
                    sociedad = table.Column<bool>(type: "boolean", nullable: false),
                    especial = table.Column<bool>(type: "boolean", nullable: false),
                    galapagos = table.Column<bool>(type: "boolean", nullable: false),
                    actividades = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    feriados = table.Column<string>(type: "text", nullable: false),
                    confirmado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_perfil_obligaciones", x => x.usuario_id);
                    table.ForeignKey(
                        name: "fk_perfil_obligaciones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preparacion_iva",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    mes = table.Column<int>(type: "integer", nullable: false),
                    meses = table.Column<int>(type: "integer", nullable: false),
                    factor_credito = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    credito_anterior = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ajuste_debito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ajuste_credito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sustento_ajustes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preparacion_iva", x => x.id);
                    table.ForeignKey(
                        name: "fk_preparacion_iva_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preparacion_renta",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    sueldos_gravados = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    iess_personal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    retenciones_sueldo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    otros_ingresos_general = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    otros_gastos_general = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ingresos_rimpe_ajuste = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    exoneraciones = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    anticipos_renta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    beneficio_especial = table.Column<bool>(type: "boolean", nullable: false),
                    galapagos = table.Column<bool>(type: "boolean", nullable: false),
                    proyeccion = table.Column<string>(type: "text", nullable: false),
                    sustento_ajustes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preparacion_renta", x => x.id);
                    table.ForeignKey(
                        name: "fk_preparacion_renta_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "revision_fiscal_comprobantes",
                columns: table => new
                {
                    comprobante_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    renta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    codigo_sustento = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    forma_pago = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    parte_relacionada = table.Column<bool>(type: "boolean", nullable: false),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    revisado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_revision_fiscal_comprobantes", x => x.comprobante_id);
                    table.ForeignKey(
                        name: "fk_revision_fiscal_comprobantes_comprobantes_comprobante_id",
                        column: x => x.comprobante_id,
                        principalTable: "comprobantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_revision_fiscal_comprobantes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "revisiones_tributarias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    mes = table.Column<int>(type: "integer", nullable: false),
                    meses = table.Column<int>(type: "integer", nullable: false),
                    huella = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revisado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    presentado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    referencia_presentacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_revisiones_tributarias", x => x.id);
                    table.ForeignKey(
                        name: "fk_revisiones_tributarias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cargas_familiares_usuario_id_anio_identificacion",
                table: "cargas_familiares",
                columns: new[] { "usuario_id", "anio", "identificacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_preparacion_iva_usuario_id_anio_mes_meses",
                table: "preparacion_iva",
                columns: new[] { "usuario_id", "anio", "mes", "meses" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_preparacion_renta_usuario_id_anio",
                table: "preparacion_renta",
                columns: new[] { "usuario_id", "anio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_revision_fiscal_comprobantes_usuario_id",
                table: "revision_fiscal_comprobantes",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_revisiones_tributarias_usuario_id_anio_tipo",
                table: "revisiones_tributarias",
                columns: new[] { "usuario_id", "anio", "tipo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cargas_familiares");

            migrationBuilder.DropTable(
                name: "perfil_obligaciones");

            migrationBuilder.DropTable(
                name: "preparacion_iva");

            migrationBuilder.DropTable(
                name: "preparacion_renta");

            migrationBuilder.DropTable(
                name: "revision_fiscal_comprobantes");

            migrationBuilder.DropTable(
                name: "revisiones_tributarias");
        }
    }
}
