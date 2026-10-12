using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class LlavesForaneasFaltantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_vinculos_contador_solicitado_por_id",
                table: "vinculos_contador",
                column: "solicitado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_periodos_contables_cerrado_por_id",
                table: "periodos_contables",
                column: "cerrado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_facturas_emitidas_detalle_detalle_sustento_id",
                table: "facturas_emitidas_detalle",
                column: "detalle_sustento_id");

            migrationBuilder.AddForeignKey(
                name: "fk_facturas_emitidas_detalle_facturas_emitidas_detalle_detalle",
                table: "facturas_emitidas_detalle",
                column: "detalle_sustento_id",
                principalTable: "facturas_emitidas_detalle",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_periodos_contables_usuarios_cerrado_por_id",
                table: "periodos_contables",
                column: "cerrado_por_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_vinculos_contador_usuarios_solicitado_por_id",
                table: "vinculos_contador",
                column: "solicitado_por_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_facturas_emitidas_detalle_facturas_emitidas_detalle_detalle",
                table: "facturas_emitidas_detalle");

            migrationBuilder.DropForeignKey(
                name: "fk_periodos_contables_usuarios_cerrado_por_id",
                table: "periodos_contables");

            migrationBuilder.DropForeignKey(
                name: "fk_vinculos_contador_usuarios_solicitado_por_id",
                table: "vinculos_contador");

            migrationBuilder.DropIndex(
                name: "ix_vinculos_contador_solicitado_por_id",
                table: "vinculos_contador");

            migrationBuilder.DropIndex(
                name: "ix_periodos_contables_cerrado_por_id",
                table: "periodos_contables");

            migrationBuilder.DropIndex(
                name: "ix_facturas_emitidas_detalle_detalle_sustento_id",
                table: "facturas_emitidas_detalle");
        }
    }
}
