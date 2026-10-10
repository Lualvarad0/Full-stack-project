using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back.Data.Migrations
{
    /// <summary>
    /// Perfil tributario en usuarios y comprobantes con dirección (recibido/emitido).
    /// Escrita a mano: renombra comprobantes_recibidos → comprobantes conservando los datos
    /// (EF había generado DROP + CREATE).
    /// </summary>
    public partial class PerfilYDireccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_comprobantes_detalle_comprobantes_recibidos_comprobante_rec",
                table: "comprobantes_detalle");

            migrationBuilder.DropForeignKey(
                name: "fk_comprobantes_xml_comprobantes_recibidos_comprobante_recibid",
                table: "comprobantes_xml");

            // comprobantes_recibidos → comprobantes (tabla, PK, FK e índices)
            migrationBuilder.RenameTable(name: "comprobantes_recibidos", newName: "comprobantes");
            migrationBuilder.Sql("ALTER TABLE comprobantes RENAME CONSTRAINT pk_comprobantes_recibidos TO pk_comprobantes;");
            migrationBuilder.Sql("ALTER TABLE comprobantes RENAME CONSTRAINT fk_comprobantes_recibidos_usuarios_usuario_id TO fk_comprobantes_usuarios_usuario_id;");
            migrationBuilder.RenameIndex(
                name: "ix_comprobantes_recibidos_usuario_id_clave_acceso",
                table: "comprobantes",
                newName: "ix_comprobantes_usuario_id_clave_acceso");
            migrationBuilder.RenameIndex(
                name: "ix_comprobantes_recibidos_usuario_id_ruc_emisor",
                table: "comprobantes",
                newName: "ix_comprobantes_usuario_id_ruc_emisor");
            migrationBuilder.DropIndex(
                name: "ix_comprobantes_recibidos_usuario_id_fecha_emision",
                table: "comprobantes");

            // Los comprobantes existentes eran todos recibidos (gastos).
            migrationBuilder.AddColumn<string>(
                name: "direccion",
                table: "comprobantes",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Recibido");

            migrationBuilder.CreateIndex(
                name: "ix_comprobantes_usuario_id_direccion_fecha_emision",
                table: "comprobantes",
                columns: new[] { "usuario_id", "direccion", "fecha_emision" });

            migrationBuilder.RenameColumn(
                name: "comprobante_recibido_id",
                table: "comprobantes_xml",
                newName: "comprobante_id");

            migrationBuilder.RenameColumn(
                name: "comprobante_recibido_id",
                table: "comprobantes_detalle",
                newName: "comprobante_id");

            migrationBuilder.RenameIndex(
                name: "ix_comprobantes_detalle_comprobante_recibido_id",
                table: "comprobantes_detalle",
                newName: "ix_comprobantes_detalle_comprobante_id");

            migrationBuilder.AddForeignKey(
                name: "fk_comprobantes_detalle_comprobantes_comprobante_id",
                table: "comprobantes_detalle",
                column: "comprobante_id",
                principalTable: "comprobantes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_comprobantes_xml_comprobantes_comprobante_id",
                table: "comprobantes_xml",
                column: "comprobante_id",
                principalTable: "comprobantes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Perfil tributario
            migrationBuilder.AddColumn<string>(
                name: "cedula",
                table: "usuarios",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ruc",
                table: "usuarios",
                type: "character varying(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "razon_social",
                table: "usuarios",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "regimen",
                table: "usuarios",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cargas_familiares",
                table: "usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "enfermedad_catastrofica",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "cedula", table: "usuarios");
            migrationBuilder.DropColumn(name: "ruc", table: "usuarios");
            migrationBuilder.DropColumn(name: "razon_social", table: "usuarios");
            migrationBuilder.DropColumn(name: "regimen", table: "usuarios");
            migrationBuilder.DropColumn(name: "cargas_familiares", table: "usuarios");
            migrationBuilder.DropColumn(name: "enfermedad_catastrofica", table: "usuarios");

            migrationBuilder.DropForeignKey(
                name: "fk_comprobantes_detalle_comprobantes_comprobante_id",
                table: "comprobantes_detalle");

            migrationBuilder.DropForeignKey(
                name: "fk_comprobantes_xml_comprobantes_comprobante_id",
                table: "comprobantes_xml");

            migrationBuilder.RenameIndex(
                name: "ix_comprobantes_detalle_comprobante_id",
                table: "comprobantes_detalle",
                newName: "ix_comprobantes_detalle_comprobante_recibido_id");

            migrationBuilder.RenameColumn(
                name: "comprobante_id",
                table: "comprobantes_detalle",
                newName: "comprobante_recibido_id");

            migrationBuilder.RenameColumn(
                name: "comprobante_id",
                table: "comprobantes_xml",
                newName: "comprobante_recibido_id");

            // Los emitidos (ingresos) no existían en el esquema anterior.
            migrationBuilder.Sql("DELETE FROM comprobantes WHERE direccion = 'Emitido';");
            migrationBuilder.DropIndex(
                name: "ix_comprobantes_usuario_id_direccion_fecha_emision",
                table: "comprobantes");
            migrationBuilder.DropColumn(name: "direccion", table: "comprobantes");

            migrationBuilder.RenameIndex(
                name: "ix_comprobantes_usuario_id_ruc_emisor",
                table: "comprobantes",
                newName: "ix_comprobantes_recibidos_usuario_id_ruc_emisor");
            migrationBuilder.RenameIndex(
                name: "ix_comprobantes_usuario_id_clave_acceso",
                table: "comprobantes",
                newName: "ix_comprobantes_recibidos_usuario_id_clave_acceso");
            migrationBuilder.Sql("ALTER TABLE comprobantes RENAME CONSTRAINT fk_comprobantes_usuarios_usuario_id TO fk_comprobantes_recibidos_usuarios_usuario_id;");
            migrationBuilder.Sql("ALTER TABLE comprobantes RENAME CONSTRAINT pk_comprobantes TO pk_comprobantes_recibidos;");
            migrationBuilder.RenameTable(name: "comprobantes", newName: "comprobantes_recibidos");

            migrationBuilder.CreateIndex(
                name: "ix_comprobantes_recibidos_usuario_id_fecha_emision",
                table: "comprobantes_recibidos",
                columns: new[] { "usuario_id", "fecha_emision" });

            migrationBuilder.AddForeignKey(
                name: "fk_comprobantes_detalle_comprobantes_recibidos_comprobante_rec",
                table: "comprobantes_detalle",
                column: "comprobante_recibido_id",
                principalTable: "comprobantes_recibidos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_comprobantes_xml_comprobantes_recibidos_comprobante_recibid",
                table: "comprobantes_xml",
                column: "comprobante_recibido_id",
                principalTable: "comprobantes_recibidos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
