using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedApp.Migrations
{
    public partial class ThayDoi28_7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenCuaNhap",
                table: "Nhap");

            migrationBuilder.DropColumn(
                name: "TenCuaXuat",
                table: "dulieuxuat");

            migrationBuilder.DropColumn(
                name: "BienSo",
                table: "ChitietXuat");

            migrationBuilder.DropColumn(
                name: "TenCua",
                table: "ChitietXuat");

            migrationBuilder.DropColumn(
                name: "BienSo",
                table: "ChitietNhap");

            migrationBuilder.AlterColumn<int>(
                name: "PhutNhap",
                table: "Nhap",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "GioNhap",
                table: "Nhap",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PhutXuat",
                table: "dulieuxuat",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "GioXuat",
                table: "dulieuxuat",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PhutNhap",
                table: "Nhap",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "GioNhap",
                table: "Nhap",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenCuaNhap",
                table: "Nhap",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "PhutXuat",
                table: "dulieuxuat",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "GioXuat",
                table: "dulieuxuat",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenCuaXuat",
                table: "dulieuxuat",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BienSo",
                table: "ChitietXuat",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenCua",
                table: "ChitietXuat",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BienSo",
                table: "ChitietNhap",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
