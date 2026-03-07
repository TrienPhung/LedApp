using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedApp.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuaNhap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mota = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuaNhap", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CuaXuat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mota = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuaXuat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "nguoiDungs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tels = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quyen = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nguoiDungs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nhap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuaNhapId = table.Column<int>(type: "int", nullable: false),
                    TenCuaNhap = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BienSoXe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayNhap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioNhap = table.Column<int>(type: "int", nullable: false),
                    PhutNhap = table.Column<int>(type: "int", nullable: false),
                    CongVao = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nhap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nhap_CuaNhap_CuaNhapId",
                        column: x => x.CuaNhapId,
                        principalTable: "CuaNhap",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dulieuxuat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuaXuatId = table.Column<int>(type: "int", nullable: false),
                    TenCuaXuat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BienSoXe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayXuat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioXuat = table.Column<int>(type: "int", nullable: false),
                    PhutXuat = table.Column<int>(type: "int", nullable: false),
                    CongRa = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dulieuxuat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dulieuxuat_CuaXuat_CuaXuatId",
                        column: x => x.CuaXuatId,
                        principalTable: "CuaXuat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChitietNhap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhapId = table.Column<int>(type: "int", nullable: false),
                    BienSo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Soluong = table.Column<long>(type: "bigint", nullable: false),
                    donvi = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChitietNhap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChitietNhap_Nhap_NhapId",
                        column: x => x.NhapId,
                        principalTable: "Nhap",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChitietXuat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenCua = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BienSo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoluongCH = table.Column<long>(type: "bigint", nullable: false),
                    donviCH = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoluongD = table.Column<long>(type: "bigint", nullable: false),
                    donviD = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    dulieuxuatId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChitietXuat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChitietXuat_dulieuxuat_dulieuxuatId",
                        column: x => x.dulieuxuatId,
                        principalTable: "dulieuxuat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChitietNhap_NhapId",
                table: "ChitietNhap",
                column: "NhapId");

            migrationBuilder.CreateIndex(
                name: "IX_ChitietXuat_dulieuxuatId",
                table: "ChitietXuat",
                column: "dulieuxuatId");

            migrationBuilder.CreateIndex(
                name: "IX_dulieuxuat_CuaXuatId",
                table: "dulieuxuat",
                column: "CuaXuatId");

            migrationBuilder.CreateIndex(
                name: "IX_Nhap_CuaNhapId",
                table: "Nhap",
                column: "CuaNhapId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChitietNhap");

            migrationBuilder.DropTable(
                name: "ChitietXuat");

            migrationBuilder.DropTable(
                name: "nguoiDungs");

            migrationBuilder.DropTable(
                name: "Nhap");

            migrationBuilder.DropTable(
                name: "dulieuxuat");

            migrationBuilder.DropTable(
                name: "CuaNhap");

            migrationBuilder.DropTable(
                name: "CuaXuat");
        }
    }
}
