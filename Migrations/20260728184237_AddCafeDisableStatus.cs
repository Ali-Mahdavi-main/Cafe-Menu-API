using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeMenu.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCafeDisableStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CafeDisableStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CafeId = table.Column<int>(type: "int", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    DisabledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisabledBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeDisableStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CafeDisableStatuses_Cafes_CafeId",
                        column: x => x.CafeId,
                        principalTable: "Cafes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CafeDisableStatuses_CafeId",
                table: "CafeDisableStatuses",
                column: "CafeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CafeDisableStatuses");
        }
    }
}
