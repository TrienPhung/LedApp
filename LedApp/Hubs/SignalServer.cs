using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using LedApp.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace LedApp.Hubs
{
    public class SignalServer:Hub
    {
        private readonly ApplicationDBContext dbContext;
        private readonly xexuatRepository xerepo;
        private readonly xuatRespository xrepo;
        private readonly CuaXuatResposity cuaXuatRepo;
        private readonly NhapRespository nhapRepo;
        private readonly ChitietNhapReposity chitietnhapRepo;
        private readonly cuanhapResposity cuaNhapRepo;
        // private int id;
        public SignalServer(ApplicationDBContext dbContext)
        {
            this.dbContext = dbContext;
            xerepo = new xexuatRepository(dbContext);
            xrepo = new xuatRespository(dbContext);
            cuaXuatRepo = new CuaXuatResposity(dbContext);
            cuaNhapRepo = new cuanhapResposity(dbContext);
            chitietnhapRepo = new ChitietNhapReposity(dbContext);
            nhapRepo = new NhapRespository(dbContext);
          //  this.xerepo = xerepo;
        }


        /*Đẩy dữ liệu ra cửa xuất*/
        /*Đọc dữ liệu liệu xuất theo mã */
        public async Task Sendxuat(int? id)
        {
            var x = xrepo.GetXuat(id);
            await Clients.All.SendAsync("Receivedxuat", x, id);
        }
        /*Đọc chi tiết dữ liệu xuất*/
        public async Task Sendxexuat(int id)
        {
            var x = xrepo.GetXuat(id);
            if(x == null)
            {
                await Clients.All.SendAsync("Receivedxexuat", null);
            }
            else
            {
                var xexuat = xerepo.Getxexuat(x.Id);
                await Clients.All.SendAsync("Receivedxexuat", xexuat,id);
            }    
            
        }
       
        public async Task SenCuaXuatSL()
        {
            var sl = cuaXuatRepo.CuaXuatSL();
            await Clients.All.SendAsync("ReceivedCuaXuatSL", sl);
        }

        /*Đẩy dữ liệu ra cửa nhập*/
        /*Đọc dữ liệu liệu nhập theo mã */
        public async Task SendNhap(int? id)
        {
            var x = nhapRepo.GetNhap(id);
            await Clients.All.SendAsync("ReceivedNhap", x, id);
        }
        /*Đọc chi tiết dữ liệu xuất*/
        public async Task SendChitietNhap(int id)
        {
            var x = nhapRepo.GetNhap(id);
            if (x == null)
            {
                await Clients.All.SendAsync("ReceivedChitietNhap", null);
            }
            else
            {
                var ct = chitietnhapRepo.GetChiTietNhap(x.Id);
                await Clients.All.SendAsync("ReceivedChitietNhap", ct, id);
            }

        }

        public async Task SenCuaNhapSL()
        {
            var sl = cuaNhapRepo.CuaNhapSL();
            await Clients.All.SendAsync("ReceivedCuaNhapSL", sl);
        }

        /*Tổng hợp xuất 1*/
        public async Task SendTongHopXuat()
        {
            var x = xrepo.GetAllXuat();
            await Clients.All.SendAsync("ReceivedTongHopXuat", x);
        }


        //Hàm thêm
        /*Tổng hợp nhập*/
        public async Task SendTongHopNhap()
        {
            var x = nhapRepo.GetAllNhap();
            if (x == null || !x.Any()) // ← kiểm tra null trước
            {
                await Clients.All.SendAsync("ReceivedTongHopNhap", new List<Nhap>());
                return;
            }
            await Clients.All.SendAsync("ReceivedTongHopNhap", x);
        }
    }
}
    