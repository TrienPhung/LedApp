using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viettel_post_API.Migrations
{
    public partial class api : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhSachXes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BienSo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenLaiXe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsHienThi = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhSachXes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChuyenXes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DanhSachXeId = table.Column<int>(type: "int", nullable: false),
                    MaChuyenApi = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LanThu = table.Column<int>(type: "int", nullable: false),
                    NgayDuKien = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianDen = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiGianHoanThanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianSync = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLatest = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuyenXes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChuyenXes_DanhSachXes_DanhSachXeId",
                        column: x => x.DanhSachXeId,
                        principalTable: "DanhSachXes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HangHoaItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChuyenXeId = table.Column<int>(type: "int", nullable: false),
                    DonVi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoLuong = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HangHoaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HangHoaItems_ChuyenXes_ChuyenXeId",
                        column: x => x.ChuyenXeId,
                        principalTable: "ChuyenXes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChuyenXes_DanhSachXeId",
                table: "ChuyenXes",
                column: "DanhSachXeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChuyenXes_MaChuyenApi",
                table: "ChuyenXes",
                column: "MaChuyenApi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaItems_ChuyenXeId",
                table: "HangHoaItems",
                column: "ChuyenXeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HangHoaItems");

            migrationBuilder.DropTable(
                name: "ChuyenXes");

            migrationBuilder.DropTable(
                name: "DanhSachXes");
        }
    }
}
