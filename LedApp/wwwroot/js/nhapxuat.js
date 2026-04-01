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
function InvokeTongHopXuat() {
    
    connection.invoke("SendTongHopXuat").catch(function (err) {
        return console.log("Lỗi gọi hàm" + err.toString());
    });
}

connection.on("ReceivedTongHopXuat", function (tonghop) {
    BindTongHopXuattoTable(tonghop);
});
function BindTongHopXuattoTable(tonghop) {
    var today = new Date();
    var hour = today.getHours();
    var cMin = today.getMinutes() + hour * 60;
   // console.log(cMin);
    $("#led4").empty();
    var tr;
    var s;
    //console.log(xes);
    $.each(tonghop, function (index, t) {
        s = "";
        $.each(t.chitietXuat, function (index1, c) {
            s += c.soluongCH + "" + c.donviCH + " ";
        });
        var tong = t.gioXuat * 60 + t.phutXuat;
        console.log(tong);
        if (tong - cMin > 0) {
            tr += `<tr>
                                    <td>X-0${t.cuaXuatId}</td>
                                    <td>${t.bienSoXe}</td>
                                    <td>${s}</td>
                                    <td>${t.gioXuat}h ${t.phutXuat}</td>                       
                               </tr>`
        }
        else {
            tr += `<tr class='trbackground'>
                                    <td>X-0${t.cuaXuatId}</td>
                                    <td>${t.bienSoXe}</td>
                                    <td>${s}</td>
                                    <td>${t.gioXuat}h ${t.phutXuat}</td>                       
                               </tr>`
        }
        
    });
    $("#led4").html(tr);
}
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
function BindxuattoTable(x,id) {
    var tenxe = "#tenxe_" + id;
    $(tenxe).empty();
    var px;
    px = `Xe: ${x.bienSoXe} Thời gian: ${x.gioXuat} giờ ${x.phutXuat} phút`
    var tong = x.gioXuat * 60 + x.phutXuat;
    var today = new Date();
    var hour = today.getHours();
    var cMin = today.getMinutes() + hour * 60;
    if (tong - cMin < 0) {
        $(tenxe).addClass("highlight").html(px);
    }
    else {
        $(tenxe).removeClass("highlight").html(px);
    }
   
}
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
//function BindNhaptoTable(x, id) {
//    var tenxe = "#tenxenhap_" + id;
//    $(tenxe).empty();
//    var px;
//    px = `Xe: ${x.bienSoXe} Thời gian: ${x.gioNhap} giờ ${x.phutNhap} phút`
//    var tong = x.gioNhap * 60 + x.phutNhap;
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

function BindNhaptoTable(x, id) {
    var tenxe = "#tenxenhap_" + id;
    var giovao = "#giovao_" + id;
    var thoigianco = "#thoigianco_" + id;
    var strip = "#strip_" + id;

    var today = new Date();
    var hour = today.getHours();
    var cMin = today.getMinutes() + hour * 60;
    var tong = x.gioNhap * 60 + x.phutNhap;
    var conLai = tong - cMin;

    // Tên xe
    $(tenxe).html("Xe: " + x.bienSoXe);

    // Giờ vào
    $(giovao).html(
        String(x.gioNhap).padStart(2, '0') + ":" + String(x.phutNhap).padStart(2, '0')
    );

    // Thời gian còn lại
    if (conLai > 0) {
        $(thoigianco).html(conLai + " phút");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" }).html("Đang chờ bàn giao");
    } else {
        $(thoigianco).html("Đã quá giờ");
        $(strip).css({ background: "#4a0c0c", color: "#fca5a5" }).html("Quá giờ bàn giao");
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
//function BindChitiettoTable(ct, id) {
//    //console.log("idre" + id);
//    console.log(ct);
//    var leid = "#led8_" + id;
//   // console.log(ct)
//    $(leid).empty();
//    var tr;
//    $.each(ct, function (index, xe) {
//        tr += `<tr>
//                  <td>${xe.soluong} ${xe.donvi}</td>
//                  <td>${xe.soluongD} ${xe.donviD}</td>
//               </tr>`
//    });
//    $(leid).html(tr);
//}

//<span style="color:#4ade80;">${xe.soluongD} ${xe.donviD}</span>
function BindChitiettoTable(ct, id) {
    var leid = "#led8_" + id;
    $(leid).empty();
    var tr = "";
    $.each(ct, function (index, xe) {
        tr += `
            <tr>
                <td style="width:15%; padding:2px 4px 2px 0; color:#555; white-space:nowrap; vertical-align:middle;">${xe.donvi}</td>
                <td style="width:42%; padding:2px 8px;  border-left:1px solid #222;  border-right:1px solid #222; text-align:center; vertical-align:middle;">
                    <span style="color:#facc15;">${xe.soluong} ${xe.donvi}</span>
                </td>
                <td style="width:43%; padding:2px 8px; text-align:center; vertical-align:middle; white-space:nowrap;">
                    <span style="color:#4ade80;">${xe.soluong} ${xe.donvi}</span>
                </td>
            </tr>`;
    });
    $(leid).html(tr);
}


/*Update thêm hàm*/
function InvokeTongHopNhap() {
    connection.invoke("SendTongHopNhap").catch(function (err) {
        return console.log("Lỗi gọi hàm" + err.toString());
    });
}

connection.on("ReceivedTongHopNhap", function (tonghop) {
    BindTongHopNhaptoTable(tonghop);
});

//function BindTongHopNhaptoTable(tonghop) {
//    $("#led7").empty();
//    var tr = "";
//    $.each(tonghop, function (index, t) {
//        tr += `<tr>
//                    <td>${t.sapVe}</td>
//                    <td>${t.daVe}</td>
//                    <td>${t.chuaVaoCuaNhap}</td>
//               </tr>`
//    });
//    $("#led7").html(tr);
//}


//fix động
//function BindTongHopNhaptoTable(tonghop) {
//    $("#led7").empty();

//    // Tính tổng từng loại
//    var sapVe = { xe: 0, tai: 0, kien: 0, dh: 0 };
//    var daVe = { xe: 0, tai: 0, kien: 0, dh: 0 };
//    var chuaVao = { xe: 0, tai: 0, kien: 0, dh: 0 };

//    $.each(tonghop, function (index, t) {
//        // Tùy theo trường TrangThaiXe bạn sẽ phân loại
//        // Tạm thời để vào daVe hết
//        daVe.xe++;
//        $.each(t.chitietNhaps, function (i, c) {
//            daVe.kien += c.soluong;
//        });
//    });

//    var tr = `
//        <tr>
//            <td style="text-align:right; padding:2px 8px; border-right:1px solid #222; color:#facc15;">${sapVe.xe} Xe</td>
//            <td style="text-align:right; padding:2px 8px; border-right:1px solid #222; color:#4ade80;">${daVe.xe} Xe</td>
//            <td style="text-align:right; padding:2px 8px; color:#f87171;">${chuaVao.xe} Xe</td>
//        </tr>
//        <tr>
//            <td style="text-align:right; padding:2px 8px; border-right:1px solid #222; color:#facc15;">${sapVe.tai} Tải</td>
//            <td style="text-align:right; padding:2px 8px; border-right:1px solid #222; color:#4ade80;">${daVe.tai} Tải</td>
//            <td style="text-align:right; padding:2px 8px; color:#f87171;">${chuaVao.tai} Tải</td>
//        </tr>
//        <tr>
//            <td style="text-align:right; padding:2px 8px; border-right:1px solid #222; color:#facc15;">${sapVe.kien} Kiện</td>
//            <td style="text-align:right; padding:2px 8px; border-right:1px solid #222; color:#4ade80;">${daVe.kien} Kiện</td>
//            <td style="text-align:right; padding:2px 8px; color:#f87171;">${chuaVao.kien} Kiện</td>
//        </tr>
//        <tr>
//            <td style="text-align:right; padding:2px 8px; border-right:1px solid #222; color:#facc15;">${sapVe.dh} ĐH</td>
//            <td style="text-align:right; padding:2px 8px; border-right:1px solid #222; color:#4ade80;">${daVe.dh} ĐH</td>
//            <td style="text-align:right; padding:2px 8px; color:#f87171;">${chuaVao.dh} ĐH</td>
//        </tr>
//    `;
//    $("#led7").html(tr);
//}

//fix cứng
function BindTongHopNhaptoTable(tonghop) {
    $("#led7").empty();
    var tr = `
        <tr>
            <td style="padding:2px 8px; border-right:1px solid #222;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">Xe</span>
                    <span style="color:#facc15;">2</span>
                </div>
            </td>
            <td style="padding:2px 8px; border-right:1px solid #222;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">Xe</span>
                    <span style="color:#4ade80;">1</span>
                </div>
            </td>
            <td style="padding:2px 8px;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">Xe</span>
                    <span style="color:#f87171;">0</span>
                </div>
            </td>
        </tr>
        <tr>
            <td style="padding:2px 8px; border-right:1px solid #222;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">Tải</span>
                    <span style="color:#facc15;">220</span>
                </div>
            </td>
            <td style="padding:2px 8px; border-right:1px solid #222;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">Tải</span>
                    <span style="color:#4ade80;">100</span>
                </div>
            </td>
            <td style="padding:2px 8px;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">Tải</span>
                    <span style="color:#f87171;">0</span>
                </div>
            </td>
        </tr>
        <tr>
            <td style="padding:2px 8px; border-right:1px solid #222;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">Kiện</span>
                    <span style="color:#facc15;">900</span>
                </div>
            </td>
            <td style="padding:2px 8px; border-right:1px solid #222;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">Kiện</span>
                    <span style="color:#4ade80;">123</span>
                </div>
            </td>
            <td style="padding:2px 8px;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">Kiện</span>
                    <span style="color:#f87171;">0</span>
                </div>
            </td>
        </tr>
        <tr>
            <td style="padding:2px 8px; border-right:1px solid #222;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">ĐH</span>
                    <span style="color:#facc15;">3500</span>
                </div>
            </td>
            <td style="padding:2px 8px; border-right:1px solid #222;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">ĐH</span>
                    <span style="color:#4ade80;">12345</span>
                </div>
            </td>
            <td style="padding:2px 8px;">
                <div style="display:flex; justify-content:space-between;">
                    <span style="color:#555;">ĐH</span>
                    <span style="color:#f87171;">0</span>
                </div>
            </td>
        </tr>
    `;
    $("#led7").html(tr);
}