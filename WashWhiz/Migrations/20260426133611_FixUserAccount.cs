using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WashWhiz.Migrations
{
    /// <inheritdoc />
    public partial class FixUserAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "Orders",
                newName: "LaundryOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LaundryOrderId",
                table: "Orders",
                newName: "OrderId");
        }
    }
}
