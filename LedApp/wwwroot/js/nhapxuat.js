"use strict";

var cuaxuatsl = 0;
/*$(() => {*/
 /*   setInterval(function () {
        var hours = new Date().getHours();

        $("#gio").html(hours + "H");

    }, 1000);
    setInterval(function () {
        var mins = new Date().getMinutes();
        $("#phut").html(mins + "Phút");

    }, 1000);
    
    */
    setReloadInterval(30);  //Refresh every 10 seconds. 
    //Because function takes seconds we * 1000 to convert seconds to milliseconds.
    function setReloadInterval(reloadTime) {
        if (reloadTime > 0)
           setInterval("loadTable()", (reloadTime * 1000));
    }
    function loadTable() {
        getSoLuong();
      // console.log("chạy");
      //  console.log("sl" + cuaxuatsl);
        for (var i = 1; i <= cuaxuatsl; i++) {
            InvokeXeXuat(i);
            Invokexuat(i);
        }
      
        InvokeTongHopXuat();

        //Thêm hàm tổng hợp nhập
        InvokeTongHopNhap();
        //alert("Thời gian "+ minutes);
        //loadProdData();
    }
    //Auto Refresh JQuery DataTable 

   
var connection = new signalR.HubConnectionBuilder().withUrl("/signalserver").build();
connection.start().then(function () {
    console.log('connected to hub');
    getSoLuong();
    console.log("sl" + cuaxuatsl);
    loadTable();
    var url = document.URL;
    var id = url.substring(url.lastIndexOf('/') + 1);
    if (url.indexOf("cuaxuat") != -1) {

        if (isFinite(id)) {
            InvokeXeXuat(id);
            Invokexuat(id);
       }
    }
    if (url.indexOf("cuanhap") != -1) {
        //console.log("Vào cửa nhập");
        if (isFinite(id)) {
            //console.log("id cửa nhập" + id);
            InvokeNhap(id);
            InvokeChitietNhap(id);
        }
    }
    //if (url.indexOf("tonghopxuat") != -1) {
    InvokeTongHopXuat();
    InvokeTongHopNhap();
    //}
    
    
}).catch(function (err) {
    return console.error(err.toString());
});

/**Tổng hợp xuất*/
function getSoLuong(){
    connection.invoke("SenCuaXuatSL").catch(function (err) {
        return console.log("Lỗi gọi hàm" + err.toString());
    });
} 
connection.on("ReceivedCuaXuatSL", function (sl) {
    cuaxuatsl = sl;
  //  console.log("sl = " + cuaxuatsl);
})



//function InvokeTongHopXuat() {
    
//    connection.invoke("SendTongHopXuat").catch(function (err) {
//        return console.log("Lỗi gọi hàm" + err.toString());
//    });
//}


//connection.on("ReceivedTongHopXuat", function (tonghop) {
//    BindTongHopXuattoTable(tonghop);
//});
//function BindTongHopXuattoTable(tonghop) {
//    var today = new Date();
//    var hour = today.getHours();
//    var cMin = today.getMinutes() + hour * 60;
//   // console.log(cMin);
//    $("#led4").empty();
//    var tr;
//    var s;
//    //console.log(xes);
//    $.each(tonghop, function (index, t) {
//        s = "";
//        $.each(t.chitietXuat, function (index1, c) {
//            s += c.soluongCH + "" + c.donviCH + " ";
//        });
//        var tong = t.gioXuat * 60 + t.phutXuat;
//        console.log(tong);
//        if (tong - cMin > 0) {
//            tr += `<tr>
//                                    <td>X-0${t.cuaXuatId}</td>
//                                    <td>${t.bienSoXe}</td>
//                                    <td>${s}</td>
//                                    <td>${t.gioXuat}h ${t.phutXuat}</td>                       
//                               </tr>`
//        }
//        else {
//            tr += `<tr class='trbackground'>
//                                    <td>X-0${t.cuaXuatId}</td>
//                                    <td>${t.bienSoXe}</td>
//                                    <td>${s}</td>
//                                    <td>${t.gioXuat}h ${t.phutXuat}</td>                       
//                               </tr>`
//        }
        
//    });
//    $("#led4").html(tr);
//}


/**Cửa xuất 1*/
function InvokeXeXuat(id) {
   // console.log("Nhận:" + id);
   
    try {
        var id = eval(id);
        connection.invoke("Sendxexuat", id);
    } catch (err) {
        console.error(err);
    }
  /*  connection.invoke("Sendxexuat",id).catch(function (err) {
        return console.log("Lỗi gọi hàm" + err.toString());
    });
    */
    
}

connection.on("Receivedxexuat", function (xes, id) {
    
    BindXetoTable(xes,id);
});
function BindXetoTable(xes,id) {
    console.log("idre" + id);
    var leid = "#led5_" + id;
   // console.log(xes)
    $(leid).empty();
    var tr;
    $.each(xes, function (index, xe) {
        tr += `<tr>
                  <td>${xe.soluongCH} ${xe.donviCH}</td>
                  <td>${xe.soluongD} ${xe.donviD}</td>
               </tr>`
    });
    $(leid).html(tr);
}
/* nhận dữ liệu xuất chi tiết */
function Invokexuat(id) {
    var id = eval(id);
    connection.invoke("Sendxuat",id).catch(function (err) {
        return console.log("Lỗi gọi hàm" + err.toString());
    });
}
connection.on("Receivedxuat", function (x,id) {
    BindxuattoTable(x,id);
});
//function BindxuattoTable(x,id) {
//    var tenxe = "#tenxe_" + id;
//    $(tenxe).empty();
//    var px;
//    px = `Xe: ${x.bienSoXe} Thời gian: ${x.gioXuat} giờ ${x.phutXuat} phút`
//    var tong = x.gioXuat * 60 + x.phutXuat;
//    var today = new Date();
//    var hour = today.getHours();
//    var cMin = today.getMinutes() + hour * 60;
//    if (tong - cMin < 0) {
//        $(tenxe).addClass("highlight").html(px);
//    }
//    else {
//        $(tenxe).removeClass("highlight").html(px);
//    }
   
//}
/* Hiển thị dữ liệu nhập của xe theo thời gian */
function InvokeNhap(id) {
    var id = eval(id);
    connection.invoke("SendNhap", id).catch(function (err) {
        return console.log("Lỗi gọi hàm" + err.toString());
    });
}
connection.on("ReceivedNhap", function (x, id) {
    console.log(x);
    console.log("id recie:" +id);
    BindNhaptoTable(x, id);
});
function BindNhaptoTable(x, id) {
    var tenxe = "#tenxenhap_" + id;
    var giovao = "#giovao_" + id;
    var thoigianco = "#thoigianco_" + id;
    var strip = "#strip_" + id;

    if (!x) {
        $(tenxe).html("--");
        $(giovao).html("--:--");
        $(thoigianco).html("--");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" })
            .html("Đang chờ xe vào...");
        return;
    }

    // ✅ Lấy giờ từ ThoiGianGioiHan thay vì gioNhap/phutNhap
    var gioiHan = x.thoiGianGioiHan ?? x.ThoiGianGioiHan;
    var vaoCua = x.thoiGianVaoCua ?? x.ThoiGianVaoCua;

    $(tenxe).html("Xe: " + (x.bienSoXe ?? x.BienSoXe ?? '--'));

    if (vaoCua) {
        var d = new Date(vaoCua);
        $(giovao).html(
            String(d.getHours()).padStart(2, '0') + ":" + String(d.getMinutes()).padStart(2, '0')
        );
    } else {
        $(giovao).html("--:--");
    }

    if (!vaoCua) {
        // Chưa vào cửa — chờ xe
        $(thoigianco).html("--");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" })
            .html("Đã phân công — Đang chờ xe vào cửa");
        return;
    }

    if (gioiHan) {
        var now = new Date();
        var deadline = new Date(gioiHan);
        var conLai = Math.round((deadline - now) / 60000);

        if (conLai > 0) {
            $(thoigianco).html(conLai + " phút");
            $(strip).css({ background: "#0c2a4a", color: "#93c5fd" })
                .html("Đang bàn giao — còn " + conLai + " phút");
        } else {
            $(thoigianco).html("Quá hạn");
            $(strip).css({ background: "#4a0c0c", color: "#fca5a5" })
                .html("QUÁ GIỜ QUY ĐỊNH — XỬ LÝ NGAY");
        }
    } else {
        $(thoigianco).html("--");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" })
            .html("Đang chờ xe vào cửa...");
    }
}

/*Hiển thị chi tiết dữ liệu nhập theo từng xe*/
/**Cửa Nhập*/
function InvokeChitietNhap(id) {
    // console.log("Nhận:" + id);

    try {
        var id = eval(id);
        connection.invoke("SendChitietNhap", id);
    } catch (err) {
        console.error(err);
    }
    /*  connection.invoke("Sendxexuat",id).catch(function (err) {
          return console.log("Lỗi gọi hàm" + err.toString());
      });
      */

}

connection.on("ReceivedChitietNhap", function (ct, id) {

    BindChitiettoTable(ct, id);
});
function BindChitiettoTable(ct, id) {
    var leid = "#led8_" + id;
    $(leid).empty();

    // Nếu không có chi tiết
    if (!ct || ct.length === 0) {
        $(leid).html(`<tr><td colspan="3" style="text-align:center;color:#555;">Chưa có dữ liệu</td></tr>`);
        return;
    }

    var tr = "";
    $.each(ct, function (index, c) {
        // ✅ Dùng đúng tên field từ ChitietNhap model
        var donVi = c.donVi ?? c.DonVi ?? '--';
        var chuaBG = c.chuaBG ?? c.ChuaBG ?? 0;
        var daBG = c.daBG ?? c.DaBG ?? 0;

        tr += `
            <tr>
                <td style="width:15%; padding:2px 4px 2px 0; color:#555; white-space:nowrap; vertical-align:middle;">
                    ${donVi}
                </td>
                <td style="width:42%; padding:2px 8px; border-left:1px solid #222; border-right:1px solid #222; text-align:center; vertical-align:middle;">
                    <span style="color:#facc15;">${chuaBG} ${donVi}</span>
                </td>
                <td style="width:43%; padding:2px 8px; text-align:center; vertical-align:middle; white-space:nowrap;">
                    <span style="color:#4ade80;">${daBG} ${donVi}</span>
                </td>
            </tr>`;
    });
    $(leid).html(tr);
}


const ttLabels = ["Chưa về", "Đang về", "Đã đến", "Đang nhập", "Hoàn thành"];
// ✅ Tìm theo chuyến
connection.on("TrangThaiXeUpdated", function (chuyenId, trangThai, thoiGianDen, thoiGianHoanThanh) {
    // Cập nhật DOM nếu có row (màn hình cũ)
    const row = document.querySelector(`tr[data-chuyen-id="${chuyenId}"]`);
    if (row) {
        row.dataset.tt = trangThai;
        row.querySelector(".cell-tt").innerHTML =
            `<span class="badge tt-${trangThai}">${ttLabels[trangThai]}</span>`;
        const elDen = row.querySelector(".thoigianden");
        const elXong = row.querySelector(".thoigianhoanthanh");
        if (elDen) elDen.innerText = thoiGianDen ?? "--";
        if (elXong) elXong.innerText = thoiGianHoanThanh ?? "--";
        row.classList.add("flash");
        setTimeout(() => row.classList.remove("flash"), 1500);
    }

    // ✅ Trigger reload tổng hợp nhập — luôn chạy dù có row hay không
    if (typeof InvokeTongHopNhap === 'function') {
        InvokeTongHopNhap();
    }
});

//thêm
function InvokeTongHopNhap() {
    console.log("Đang gọi SendTongHopNhapFull...");
    connection.invoke("SendTongHopNhapFull").catch(function (err) {
        return console.log("Lỗi: " + err.toString());
    });
}

connection.on("UpdateBangTongHop", function (data) {
    console.log("Nhận UpdateBangTongHop:", data);
    if (!data) return;
    BindTongHopNhaptoTable(data);
});

function BindTongHopNhaptoTable(data) {
    var tr = "";

    // Hàng Xe
    tr += rowTongHop("Xe", data.sapVeXe, data.daVeXe, data.chuaVaoXe);

    // Hàng hóa — chỉ hiện nếu có số lượng > 0 ở ít nhất 1 cột
    data.hangHoas.forEach(function (h) {
        if (h.sapVe > 0 || h.daVe > 0 || h.chuaVao > 0) {
            tr += rowTongHop(h.donVi, h.sapVe, h.daVe, h.chuaVao);
        }
    });

    $("#led7").html(tr);
    // ✅ Cập nhật thanh strip
    var strip = $("#tong-strip-nhap");
    if (data.chuaVaoXe > 0) {
        strip.css({ background: "#3a1f05", color: "#fbbf24" })
            .html("⚠ Có " + data.chuaVaoXe + " xe chưa vào cửa");
    } else if (data.sapVeXe > 0 && data.daVeXe == 0) {
        strip.css({ background: "#0c2a4a", color: "#93c5fd" })
            .html("Đang chờ xe về");
    } else {
        strip.css({ background: "#0c2a4a", color: "#93c5fd" })
            .html("Hoạt động bình thường");
    }
}

function rowTongHop(label, sv, dv, cv) {
    return `
    <tr>
        <td style="padding:2px 8px; border-right:1px solid #222;">
            <div style="display:flex; justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#facc15;">${sv}</span>
            </div>
        </td>
        <td style="padding:2px 8px; border-right:1px solid #222;">
            <div style="display:flex; justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#4ade80;">${dv}</span>
            </div>
        </td>
        <td style="padding:2px 8px;">
            <div style="display:flex; justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#f87171;">${cv}</span>
            </div>
        </td>
    </tr>`;
}


// ===== THÊM VÀO nhapxuat.js =====

// 1. Trong hàm loadTable(), thêm dòng này (thay InvokeTongHopXuat cũ):
//    InvokeTongHopXuatFull();

// 2. Trong connection.start().then(...), thêm:
//    InvokeTongHopXuatFull();

// ---- Hàm gọi Hub ----
function InvokeTongHopXuatFull() {
    connection.invoke("SendTongHopXuatFull").catch(function (err) {
        return console.log("Lỗi gọi hàm TongHopXuatFull: " + err.toString());
    });
}

// ---- Nhận dữ liệu từ Hub ----
connection.on("UpdateBangTongHopXuat", function (data) {
    if (!data) return;
    BindTongHopXuattoTable(data);
});

// ---- Render 3 cột LED tổng hợp xuất ----
function BindTongHopXuattoTable(data) {
    var tr = "";

    // Hàng Xe
    tr += rowTongHopXuat("Xe", data.choXuatXe, data.dangXuatXe, data.daRoiKhoXe);

    // Hàng hóa
    if (data.hangHoas) {
        data.hangHoas.forEach(function (h) {
            if (h.choXuat > 0 || h.dangXuat > 0 || h.daRoi > 0) {
                tr += rowTongHopXuat(h.donVi, h.choXuat, h.dangXuat, h.daRoi);
            }
        });
    }

    $("#led4-tonghop").html(tr);

    // Cập nhật strip trạng thái
    var strip = $("#tong-strip-xuat");
    if (data.dangXuatXe > 0) {
        strip.css({ background: "#3a1f05", color: "#fb923c" })
            .html("Đang xuất " + data.dangXuatXe + " xe tại cửa");
    } else if (data.choXuatXe > 0) {
        strip.css({ background: "#1a1a1a", color: "#facc15" })
            .html("Sắp xuất " + data.choXuatXe + " xe");
    } else {
        strip.css({ background: "#0f2a1a", color: "#4ade80" })
            .html("Hoạt động bình thường");
    }
}

function rowTongHopXuat(label, cho, dang, daRoi) {
    return `
    <tr>
        <td style="padding:2px 8px; border-right:1px solid #222;">
            <div style="display:flex; justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#facc15;">${cho}</span>
            </div>
        </td>
        <td style="padding:2px 8px; border-right:1px solid #222;">
            <div style="display:flex; justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#fb923c;">${dang}</span>
            </div>
        </td>
        <td style="padding:2px 8px;">
            <div style="display:flex; justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#4ade80;">${daRoi}</span>
            </div>
        </td>
    </tr>`;
}

// ===== THAY THẾ 2 HÀM NÀY TRONG nhapxuat.js =====

// ---- Hàm 1: Bind chi tiết hàng hóa (ChuaBG / DaBG) ----
// Thay thế toàn bộ hàm BindXetoTable cũ
function BindXetoTable(xes, id) {
    var leid = "#led5_" + id;
    $(leid).empty();

    if (!xes || xes.length === 0) {
        $(leid).html(`
            <tr>
                <td style="color:#555;">Tải</td>
                <td style="color:#facc15;">—</td>
                <td style="color:#555;">—</td>
            </tr>
            <tr>
                <td style="color:#555;">Kiện</td>
                <td style="color:#facc15;">—</td>
                <td style="color:#555;">—</td>
            </tr>
            <tr>
                <td style="color:#555;">ĐH</td>
                <td style="color:#facc15;">—</td>
                <td style="color:#555;">—</td>
            </tr>`);
        return;
    }

    var tr = "";
    $.each(xes, function (index, xe) {
        var donVi = xe.donVi ?? xe.DonVi ?? '--';
        var chuaBG = xe.chuaBG ?? xe.ChuaBG ?? 0;
        var daBG = xe.daBG ?? xe.DaBG ?? 0;

        tr += `
            <tr>
                <td style="color:#555;padding:2px 4px;">${donVi}</td>
                <td style="color:#facc15;text-align:center;padding:2px 4px;">${chuaBG} ${donVi}</td>
                <td style="color:#555;text-align:center;padding:2px 4px;">${daBG} ${donVi}</td>
            </tr>`;
    });
    $(leid).html(tr);
}

// ---- Hàm 2: Bind thông tin xe (biển số, giờ vào, còn lại, strip) ----
// Thay thế toàn bộ hàm BindxuattoTable cũ
function BindxuattoTable(x, id) {
    var tenxe = "#tenxe_" + id;
    var giovao = "#giovao_" + id;
    var conlai = "#conlai_" + id;
    var strip = "#strip_" + id;

    // Không có xe
    if (!x) {
        $(tenxe).html("Chờ xe ra...");
        $(giovao).html("--:--");
        $(conlai).html("—");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" })
            .html("Chờ xe ra...");
        return;
    }

    // Biển số xe
    var bienSo = x.bienSoXe ?? x.BienSoXe ?? '--';
    $(tenxe).html("Xe: " + bienSo);

    var vaoCua = x.thoiGianVaoCua ?? x.ThoiGianVaoCua;
    var gioiHan = x.thoiGianGioiHan ?? x.ThoiGianGioiHan;

    // Giờ vào
    if (vaoCua) {
        var d = new Date(vaoCua);
        $(giovao).html(
            String(d.getHours()).padStart(2, '0') + ":" +
            String(d.getMinutes()).padStart(2, '0')
        );
    } else {
        $(giovao).html("--:--");
    }

    // Chưa vào cửa
    if (!vaoCua) {
        $(conlai).css("color", "#facc15").html("—");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" })
            .html("Đã phân công — Đang chờ xe vào cửa");
        return;
    }

    // Tính còn lại
    if (gioiHan) {
        var now = new Date();
        var deadline = new Date(gioiHan);
        var conLaiPhut = Math.round((deadline - now) / 60000);

        if (conLaiPhut > 0) {
            $(conlai).css("color", "#facc15").html(conLaiPhut + " phút");
            $(strip).css({ background: "#0c2a4a", color: "#93c5fd" })
                .html("Đang xuất hàng — còn " + conLaiPhut + " phút");
        } else {
            $(conlai).css("color", "#f87171").html("Quá hạn");
            $(strip).css({ background: "#4a0c0c", color: "#fca5a5" })
                .html("QUÁ GIỜ XUẤT — XỬ LÝ NGAY");
        }
    } else {
        $(conlai).css("color", "#facc15").html("—");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" })
            .html("Đang chờ xe vào cửa...");
    }
}
