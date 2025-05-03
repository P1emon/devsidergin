using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEStore.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnDaXemToThongbao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DaXem",
                table: "ThongBaos",
                type: "bit",
                nullable: false,
                defaultValue: false);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThongBaos");
        }
    }
}
