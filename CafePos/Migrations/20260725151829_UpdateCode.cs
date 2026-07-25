using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafePos.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccess",
                table: "Payments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OrderInfo",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseCode",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionNo",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_EmployeeId",
                table: "Payments",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Employees_EmployeeId",
                table: "Payments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Employees_EmployeeId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_EmployeeId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsSuccess",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OrderInfo",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ResponseCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TransactionNo",
                table: "Payments");
        }
    }
}
