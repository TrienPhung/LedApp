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
function BindNhaptoTable(x, id) {
    var tenxe = "#tenxenhap_" + id;
    $(tenxe).empty();
    var px;
    px = `Xe: ${x.bienSoXe} Thời gian: ${x.gioNhap} giờ ${x.phutNhap} phút`
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
    //console.log("idre" + id);
    console.log(ct);
    var leid = "#led8_" + id;
   // console.log(ct)
    $(leid).empty();
    var tr;
    $.each(ct, function (index, xe) {
        tr += `<tr>
                  <td>${xe.soluong} ${xe.donvi}</td>
                  <td>${xe.soluongD} ${xe.donviD}</td>
               </tr>`
    });
    $(leid).html(tr);
}