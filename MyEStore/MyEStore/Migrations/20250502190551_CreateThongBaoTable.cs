using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEStore.Migrations
{
    /// <inheritdoc />
    public partial class CreateThongBaoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
    name: "ThongBaos",
    columns: table => new
    {
        MaTb = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
        MaKh = table.Column<string>(type: "nvarchar(max)", nullable: false),
        MaMv = table.Column<string>(type: "nvarchar(max)", nullable: false),
        MaSlider = table.Column<string>(type: "nvarchar(max)", nullable: false),
        NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
        NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: true),
        TieuDe = table.Column<string>(type: "nvarchar(max)", nullable: true)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_ThongBaos", x => x.MaTb);
    });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
    name: "ThongBaos");
        }
    }
}
