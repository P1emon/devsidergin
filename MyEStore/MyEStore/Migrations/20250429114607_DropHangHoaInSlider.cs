using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEStore.Migrations
{
    /// <inheritdoc />
    public partial class DropHangHoaInSlider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sliders_HangHoa_MaHH",
                table: "Sliders");

            migrationBuilder.DropIndex(
                name: "IX_Sliders_MaHH",
                table: "Sliders");

            migrationBuilder.DropColumn(
                name: "MaHH",
                table: "Sliders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaHH",
                table: "Sliders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sliders_MaHH",
                table: "Sliders",
                column: "MaHH");

            migrationBuilder.AddForeignKey(
                name: "FK_Sliders_HangHoa_MaHH",
                table: "Sliders",
                column: "MaHH",
                principalTable: "HangHoa",
                principalColumn: "MaHH");
        }
    }
}
