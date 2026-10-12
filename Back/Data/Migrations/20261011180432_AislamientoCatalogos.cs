using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class AislamientoCatalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_clientes_identificacion",
                table: "clientes");

            migrationBuilder.AddColumn<int>(
                name: "usuario_id",
                table: "productos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "usuario_id",
                table: "clientes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_productos_usuario_id",
                table: "productos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_usuario_id_identificacion",
                table: "clientes",
                columns: new[] { "usuario_id", "identificacion" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_clientes_usuarios_usuario_id",
                table: "clientes",
                column: "usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_productos_usuarios_usuario_id",
                table: "productos",
                column: "usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clientes_usuarios_usuario_id",
                table: "clientes");

            migrationBuilder.DropForeignKey(
                name: "fk_productos_usuarios_usuario_id",
                table: "productos");

            migrationBuilder.DropIndex(
                name: "ix_productos_usuario_id",
                table: "productos");

            migrationBuilder.DropIndex(
                name: "ix_clientes_usuario_id_identificacion",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "usuario_id",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "usuario_id",
                table: "clientes");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_identificacion",
                table: "clientes",
                column: "identificacion",
                unique: true);
        }
    }
}
