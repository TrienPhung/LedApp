using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedApp.Migrations
{
    public partial class UpdateModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChitietXuat_dulieuxuat_dulieuxuatId",
                table: "ChitietXuat");

            migrationBuilder.DropTable(
                name: "dulieuxuat");

            migrationBuilder.DropColumn(
                name: "CongVao",
                table: "Nhap");

            migrationBuilder.DropColumn(
                name: "GioNhap",
                table: "Nhap");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "nguoiDungs");

            migrationBuilder.DropColumn(
                name: "donviCH",
                table: "ChitietXuat");

            migrationBuilder.RenameColumn(
                name: "PhutNhap",
                table: "Nhap",
                newName: "NhanVienXacNhanId");

            migrationBuilder.RenameColumn(
                name: "NgayNhap",
                table: "Nhap",
                newName: "ThoiGianPhanCong");

            migrationBuilder.RenameColumn(
                name: "dulieuxuatId",
                table: "ChitietXuat",
                newName: "XuatId");

            migrationBuilder.RenameColumn(
                name: "donviD",
                table: "ChitietXuat",
                newName: "DonVi");

            migrationBuilder.RenameColumn(
                name: "SoluongD",
                table: "ChitietXuat",
                newName: "DaBG");

            migrationBuilder.RenameColumn(
                name: "SoluongCH",
                table: "ChitietXuat",
                newName: "ChuaBG");

            migrationBuilder.RenameIndex(
                name: "IX_ChitietXuat_dulieuxuatId",
                table: "ChitietXuat",
                newName: "IX_ChitietXuat_XuatId");

            migrationBuilder.RenameColumn(
                name: "donvi",
                table: "ChitietNhap",
                newName: "DonVi");

            migrationBuilder.RenameColumn(
                name: "Soluong",
                table: "ChitietNhap",
                newName: "DaBG");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "Nhap",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianGioiHan",
                table: "Nhap",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianHoanThanh",
                table: "Nhap",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianVaoBai",
                table: "Nhap",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianVaoCua",
                table: "Nhap",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "nguoiDungs",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "ChuaBG",
                table: "ChitietNhap",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "CanhBao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoaiPhieu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhieuId = table.Column<int>(type: "int", nullable: false),
                    LoaiCanhBao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanhBao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DanhSachXe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BienSoXe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiXe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaiTrong = table.Column<long>(type: "bigint", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiGianDuKienVe = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhSachXe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LichSuBanGiao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoaiPhieu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhieuId = table.Column<int>(type: "int", nullable: false),
                    ChiTietDonViId = table.Column<int>(type: "int", nullable: false),
                    DonVi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoBG = table.Column<long>(type: "bigint", nullable: false),
                    NhanVienId = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuBanGiao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichSuBanGiao_nguoiDungs_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "nguoiDungs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Xuat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuaXuatId = table.Column<int>(type: "int", nullable: false),
                    XeId = table.Column<int>(type: "int", nullable: false),
                    ThoiGianPhanCong = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianVaoBai = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiGianVaoCua = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiGianGioiHan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiGianHoanThanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiGianXuatPhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NhanVienXacNhanId = table.Column<int>(type: "int", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Xuat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Xuat_CuaXuat_CuaXuatId",
                        column: x => x.CuaXuatId,
                        principalTable: "CuaXuat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Xuat_DanhSachXe_XeId",
                        column: x => x.XeId,
                        principalTable: "DanhSachXe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Xuat_nguoiDungs_NhanVienXacNhanId",
                        column: x => x.NhanVienXacNhanId,
                        principalTable: "nguoiDungs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nhap_NhanVienXacNhanId",
                table: "Nhap",
                column: "NhanVienXacNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuBanGiao_NhanVienId",
                table: "LichSuBanGiao",
                column: "NhanVienId");

            migrationBuilder.CreateIndex(
                name: "IX_Xuat_CuaXuatId",
                table: "Xuat",
                column: "CuaXuatId");

            migrationBuilder.CreateIndex(
                name: "IX_Xuat_NhanVienXacNhanId",
                table: "Xuat",
                column: "NhanVienXacNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_Xuat_XeId",
                table: "Xuat",
                column: "XeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChitietXuat_Xuat_XuatId",
                table: "ChitietXuat",
                column: "XuatId",
                principalTable: "Xuat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nhap_nguoiDungs_NhanVienXacNhanId",
                table: "Nhap",
                column: "NhanVienXacNhanId",
                principalTable: "nguoiDungs",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChitietXuat_Xuat_XuatId",
                table: "ChitietXuat");

            migrationBuilder.DropForeignKey(
                name: "FK_Nhap_nguoiDungs_NhanVienXacNhanId",
                table: "Nhap");

            migrationBuilder.DropTable(
                name: "CanhBao");

            migrationBuilder.DropTable(
                name: "LichSuBanGiao");

            migrationBuilder.DropTable(
                name: "Xuat");

            migrationBuilder.DropTable(
                name: "DanhSachXe");

            migrationBuilder.DropIndex(
                name: "IX_Nhap_NhanVienXacNhanId",
                table: "Nhap");

            migrationBuilder.DropColumn(
                name: "ThoiGianGioiHan",
                table: "Nhap");

            migrationBuilder.DropColumn(
                name: "ThoiGianHoanThanh",
                table: "Nhap");

            migrationBuilder.DropColumn(
                name: "ThoiGianVaoBai",
                table: "Nhap");

            migrationBuilder.DropColumn(
                name: "ThoiGianVaoCua",
                table: "Nhap");

            migrationBuilder.DropColumn(
                name: "ChuaBG",
                table: "ChitietNhap");

            migrationBuilder.RenameColumn(
                name: "ThoiGianPhanCong",
                table: "Nhap",
                newName: "NgayNhap");

            migrationBuilder.RenameColumn(
                name: "NhanVienXacNhanId",
                table: "Nhap",
                newName: "PhutNhap");

            migrationBuilder.RenameColumn(
                name: "XuatId",
                table: "ChitietXuat",
                newName: "dulieuxuatId");

            migrationBuilder.RenameColumn(
                name: "DonVi",
                table: "ChitietXuat",
                newName: "donviD");

            migrationBuilder.RenameColumn(
                name: "DaBG",
                table: "ChitietXuat",
                newName: "SoluongD");

            migrationBuilder.RenameColumn(
                name: "ChuaBG",
                table: "ChitietXuat",
                newName: "SoluongCH");

            migrationBuilder.RenameIndex(
                name: "IX_ChitietXuat_XuatId",
                table: "ChitietXuat",
                newName: "IX_ChitietXuat_dulieuxuatId");

            migrationBuilder.RenameColumn(
                name: "DonVi",
                table: "ChitietNhap",
                newName: "donvi");

            migrationBuilder.RenameColumn(
                name: "DaBG",
                table: "ChitietNhap",
                newName: "Soluong");

            migrationBuilder.AlterColumn<bool>(
                name: "TrangThai",
                table: "Nhap",
                type: "bit",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "CongVao",
                table: "Nhap",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GioNhap",
                table: "Nhap",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "nguoiDungs",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "nguoiDungs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "donviCH",
                table: "ChitietXuat",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "dulieuxuat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuaXuatId = table.Column<int>(type: "int", nullable: false),
                    BienSoXe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CongRa = table.Column<int>(type: "int", nullable: false),
                    GioXuat = table.Column<int>(type: "int", nullable: true),
                    NgayXuat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PhutXuat = table.Column<int>(type: "int", nullable: true),
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

            migrationBuilder.CreateIndex(
                name: "IX_dulieuxuat_CuaXuatId",
                table: "dulieuxuat",
                column: "CuaXuatId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChitietXuat_dulieuxuat_dulieuxuatId",
                table: "ChitietXuat",
                column: "dulieuxuatId",
                principalTable: "dulieuxuat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
