"use strict";

var cuaxuatsl = 0;

setReloadInterval(30);

function setReloadInterval(reloadTime) {
    if (reloadTime > 0)
        setInterval(function () { loadTable(); }, reloadTime * 1000);
}

function loadTable() {
    getSoLuong();
    for (var i = 1; i <= cuaxuatsl; i++) {
        InvokeXeXuat(i);
        Invokexuat(i);
    }
    InvokeTongHopXuatFull();
    InvokeTongHopNhap();
}

var connection = new signalR.HubConnectionBuilder().withUrl("/signalserver").build();

connection.start().then(function () {
    console.log('connected to hub');

    getSoLuong();
    InvokeTongHopXuatFull();
    InvokeTongHopNhap();

    var url = document.URL;
    var id = url.substring(url.lastIndexOf('/') + 1);

    if (url.indexOf("cuaxuat") !== -1 && isFinite(id) && id !== '') {
        var cuaId = parseInt(id, 10);
        // Gọi ngay lập tức
        InvokeXeXuat(cuaId);
        Invokexuat(cuaId);
        // Gọi lại sau 500ms phòng lần đầu chưa kịp render
        setTimeout(function () {
            InvokeXeXuat(cuaId);
            Invokexuat(cuaId);
        }, 500);
        // Gọi lại sau 2s phòng server còn warm up
        setTimeout(function () {
            InvokeXeXuat(cuaId);
            Invokexuat(cuaId);
        }, 2000);
        // Poll định kỳ 15s
        setInterval(function () {
            InvokeXeXuat(cuaId);
            Invokexuat(cuaId);
        }, 15000);
    }

    if (url.indexOf("cuanhap") !== -1 && isFinite(id) && id !== '') {
        var nhapId = parseInt(id, 10);
        console.log("[DEBUG] Đang ở cửa nhập id:", nhapId);
        InvokeNhap(nhapId);
        InvokeChitietNhap(nhapId);
    }

}).catch(function (err) {
    return console.error(err.toString());
});

// ==================== TỔNG HỢP XUẤT ====================

function getSoLuong() {
    connection.invoke("SenCuaXuatSL").catch(function (err) {
        console.log("Lỗi gọi SenCuaXuatSL: " + err.toString());
    });
}

connection.on("ReceivedCuaXuatSL", function (sl) {
    console.log("[DEBUG] ReceivedCuaXuatSL nhận được:", sl, "| kiểu:", typeof sl);
    cuaxuatsl = sl;
});

function InvokeTongHopXuatFull() {
    connection.invoke("SendTongHopXuatFull").catch(function (err) {
        console.log("Lỗi gọi SendTongHopXuatFull: " + err.toString());
    });
}

connection.on("UpdateBangTongHopXuat", function (data) {
    if (!data) return;
    BindTongHopXuattoTable(data);
});

function BindTongHopXuattoTable(data) {
    var tr = "";
    // Chỉ hiện hàng Xe nếu có ít nhất 1 số > 0
    if ((data.choXuatXe || 0) + (data.dangXuatXe || 0) + (data.daRoiKhoXe || 0) > 0) {
        tr += rowTongHopXuat("Xe", data.choXuatXe, data.dangXuatXe, data.daRoiKhoXe);
    }
    if (data.hangHoas) {
        data.hangHoas.forEach(function (h) {
            if (h.choXuat > 0 || h.dangXuat > 0 || h.daRoi > 0) {
                tr += rowTongHopXuat(h.donVi, h.choXuat, h.dangXuat, h.daRoi);
            }
        });
    }
    if (!tr) {
        tr = '<tr><td colspan="3" style="text-align:center;color:#333;padding:16px;font-style:italic;">— Chưa có lịch xuất —</td></tr>';
    }
    $("#led4-tonghop").html(tr);

    var strip = $("#tong-strip-xuat");
    var tong = (data.choXuatXe || 0) + (data.dangXuatXe || 0) + (data.daRoiKhoXe || 0);

    if (data.dangXuatXe > 0) {
        strip.css({ background: "#3a1f05", color: "#fb923c" })
            .html("Đang xuất " + data.dangXuatXe + " xe tại cửa");
    } else if (data.choXuatXe > 0) {
        strip.css({ background: "#1a1a1a", color: "#facc15" })
            .html("Sắp xuất " + data.choXuatXe + " xe");
    } else if (tong === 0) {
        strip.css({ background: "#1a1a1a", color: "#475569" })
            .html("Chưa có lịch xuất hôm nay");
    } else {
        // tong > 0 nhưng tất cả đã rời kho
        strip.css({ background: "#0f2a1a", color: "#4ade80" })
            .html("Tất cả xe đã rời kho");
    }
}

function rowTongHopXuat(label, cho, dang, daRoi) {
    return `
    <tr>
        <td style="padding:2px 8px;border-right:1px solid #222;">
            <div style="display:flex;justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#facc15;">${cho}</span>
            </div>
        </td>
        <td style="padding:2px 8px;border-right:1px solid #222;">
            <div style="display:flex;justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#fb923c;">${dang}</span>
            </div>
        </td>
        <td style="padding:2px 8px;">
            <div style="display:flex;justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#4ade80;">${daRoi}</span>
            </div>
        </td>
    </tr>`;
}

// ==================== CỬA XUẤT ====================

function InvokeXeXuat(id) {
    var cuaId = parseInt(id, 10);
    console.log("[DEBUG] Gọi Sendxexuat cửa:", cuaId);
    connection.invoke("Sendxexuat", cuaId).catch(function (err) {
        console.error("Lỗi Sendxexuat:", err);
    });
}

connection.on("Receivedxexuat", function (xes, id) {
    BindXetoTable(xes, id);
});

function BindXetoTable(xes, id) {
    var leid = "#led5_" + id;

    if (!xes || xes.length === 0) {
        $(leid).html(`
            <tr>
                <td style="color:#555;padding:2px 4px;width:18%;">Tải</td>
                <td style="color:#333;text-align:center;border-left:1px solid #222;border-right:1px solid #222;">—</td>
                <td style="color:#333;text-align:center;">—</td>
            </tr>
            <tr>
                <td style="color:#555;padding:2px 4px;">Kiện</td>
                <td style="color:#333;text-align:center;border-left:1px solid #222;border-right:1px solid #222;">—</td>
                <td style="color:#333;text-align:center;">—</td>
            </tr>
            <tr>
                <td style="color:#555;padding:2px 4px;">ĐH</td>
                <td style="color:#333;text-align:center;border-left:1px solid #222;border-right:1px solid #222;">—</td>
                <td style="color:#333;text-align:center;">—</td>
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
                <td style="color:#555;padding:2px 4px;width:18%;white-space:nowrap;">${donVi}</td>
                <td style="color:#facc15;text-align:center;padding:2px 4px;
                           border-left:1px solid #222;border-right:1px solid #222;">
                    ${chuaBG} ${donVi}
                </td>
                <td style="color:#555;text-align:center;padding:2px 4px;">
                    ${daBG} ${donVi}
                </td>
            </tr>`;
    });
    $(leid).html(tr);
}

function Invokexuat(id) {
    var cuaId = parseInt(id, 10);
    console.log("[DEBUG] Gọi Sendxuat cửa:", cuaId);
    connection.invoke("Sendxuat", cuaId).catch(function (err) {
        console.log("Lỗi gọi Sendxuat: " + err.toString());
    });
}

connection.on("Receivedxuat", function (x, id) {
    BindxuattoTable(x, id);
});

function BindxuattoTable(x, id) {
    var tenxe = "#tenxe_" + id;
    var giovao = "#giovao_" + id;
    var conlai = "#conlai_" + id;
    var strip = "#strip_" + id;

    if (!x || x.trangThai === 3 || x.trangThai === 4) {
        $(tenxe).html("Chờ xe ra...");
        $(giovao).html("--:--");
        $(conlai).html("—");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" }).html("Chờ xe ra...");
        return;
    }

    var bienSo = x.bienSoXe ?? x.BienSoXe ?? '--';
    var vaoCua = x.thoiGianVaoCua ?? x.ThoiGianVaoCua;
    var gioiHan = x.thoiGianGioiHan ?? x.ThoiGianGioiHan;

    $(tenxe).html("Xe: " + bienSo);

    if (vaoCua) {
        var d = new Date(vaoCua);
        $(giovao).html(String(d.getHours()).padStart(2, '0') + ":" + String(d.getMinutes()).padStart(2, '0'));
    } else {
        $(giovao).html("--:--");
    }

    if (!vaoCua) {
        $(conlai).css("color", "#facc15").html("—");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" }).html("Đã phân công — Đang chờ xe vào cửa");
        return;
    }

    if (gioiHan) {
        var now = new Date();
        var deadline = new Date(gioiHan);
        var conLaiPhut = Math.round((deadline - now) / 60000);
        if (conLaiPhut > 0) {
            $(conlai).css("color", "#facc15").html(conLaiPhut + " phút");
            $(strip).css({ background: "#0c2a4a", color: "#93c5fd" }).html("Đang xuất hàng — còn " + conLaiPhut + " phút");
        } else {
            $(conlai).css("color", "#f87171").html("Quá hạn");
            $(strip).css({ background: "#4a0c0c", color: "#fca5a5" }).html("QUÁ GIỜ XUẤT — XỬ LÝ NGAY");
        }
    } else {
        $(conlai).css("color", "#facc15").html("—");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" }).html("Đang chờ xe vào cửa...");
    }
}

// ==================== CỬA NHẬP ====================

function InvokeNhap(id) {
    var nhapId = parseInt(id, 10);
    connection.invoke("SendNhap", nhapId).catch(function (err) {
        console.log("Lỗi gọi SendNhap: " + err.toString());
    });
}

connection.on("ReceivedNhap", function (x, id) {
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
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" }).html("Đang chờ xe vào...");
        return;
    }

    var gioiHan = x.thoiGianGioiHan ?? x.ThoiGianGioiHan;
    var vaoCua = x.thoiGianVaoCua ?? x.ThoiGianVaoCua;

    $(tenxe).html("Xe: " + (x.bienSoXe ?? x.BienSoXe ?? '--'));

    if (vaoCua) {
        var d = new Date(vaoCua);
        $(giovao).html(String(d.getHours()).padStart(2, '0') + ":" + String(d.getMinutes()).padStart(2, '0'));
    } else {
        $(giovao).html("--:--");
    }

    if (!vaoCua) {
        $(thoigianco).html("--");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" }).html("Đã phân công — Đang chờ xe vào cửa");
        return;
    }

    if (gioiHan) {
        var now = new Date();
        var deadline = new Date(gioiHan);
        var conLai = Math.round((deadline - now) / 60000);
        if (conLai > 0) {
            $(thoigianco).html(conLai + " phút");
            $(strip).css({ background: "#0c2a4a", color: "#93c5fd" }).html("Đang bàn giao — còn " + conLai + " phút");
        } else {
            $(thoigianco).html("Quá hạn");
            $(strip).css({ background: "#4a0c0c", color: "#fca5a5" }).html("QUÁ GIỜ QUY ĐỊNH — XỬ LÝ NGAY");
        }
    } else {
        $(thoigianco).html("--");
        $(strip).css({ background: "#0c2a4a", color: "#93c5fd" }).html("Đang chờ xe vào cửa...");
    }
}

function InvokeChitietNhap(id) {
    var nhapId = parseInt(id, 10);
    connection.invoke("SendChitietNhap", nhapId).catch(function (err) {
        console.error("Lỗi SendChitietNhap:", err);
    });
}

connection.on("ReceivedChitietNhap", function (ct, id) {
    BindChitiettoTable(ct, id);
});

function BindChitiettoTable(ct, id) {
    var leid = "#led8_" + id;
    $(leid).empty();

    if (!ct || ct.length === 0) {
        $(leid).html(`<tr><td colspan="3" style="text-align:center;color:#555;">Chưa có dữ liệu</td></tr>`);
        return;
    }

    var tr = "";
    $.each(ct, function (index, c) {
        var donVi = c.donVi ?? c.DonVi ?? '--';
        var chuaBG = c.chuaBG ?? c.ChuaBG ?? 0;
        var daBG = c.daBG ?? c.DaBG ?? 0;
        tr += `
            <tr>
                <td style="width:15%;padding:2px 4px 2px 0;color:#555;white-space:nowrap;vertical-align:middle;">
                    ${donVi}
                </td>
                <td style="width:42%;padding:2px 8px;border-left:1px solid #222;border-right:1px solid #222;text-align:center;vertical-align:middle;">
                    <span style="color:#facc15;">${chuaBG} ${donVi}</span>
                </td>
                <td style="width:43%;padding:2px 8px;text-align:center;vertical-align:middle;white-space:nowrap;">
                    <span style="color:#4ade80;">${daBG} ${donVi}</span>
                </td>
            </tr>`;
    });
    $(leid).html(tr);
}

// ==================== TỔNG HỢP NHẬP ====================

function InvokeTongHopNhap() {
    connection.invoke("SendTongHopNhapFull").catch(function (err) {
        console.log("Lỗi SendTongHopNhapFull: " + err.toString());
    });
}

connection.on("UpdateBangTongHop", function (data) {
    if (!data) return;
    BindTongHopNhaptoTable(data);
});

function BindTongHopNhaptoTable(data) {
    var tr = "";
    tr += rowTongHop("Xe", data.sapVeXe, data.daVeXe, data.chuaVaoXe);
    data.hangHoas.forEach(function (h) {
        if (h.sapVe > 0 || h.daVe > 0 || h.chuaVao > 0) {
            tr += rowTongHop(h.donVi, h.sapVe, h.daVe, h.chuaVao);
        }
    });
    $("#led7").html(tr);

    var strip = $("#tong-strip-nhap");
    if (data.chuaVaoXe > 0) {
        strip.css({ background: "#3a1f05", color: "#fbbf24" }).html("⚠ Có " + data.chuaVaoXe + " xe chưa vào cửa");
    } else if (data.sapVeXe > 0 && data.daVeXe == 0) {
        strip.css({ background: "#0c2a4a", color: "#93c5fd" }).html("Đang chờ xe về");
    } else {
        strip.css({ background: "#0c2a4a", color: "#93c5fd" }).html("Hoạt động bình thường");
    }
}

function rowTongHop(label, sv, dv, cv) {
    return `
    <tr>
        <td style="padding:2px 8px;border-right:1px solid #222;">
            <div style="display:flex;justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#facc15;">${sv}</span>
            </div>
        </td>
        <td style="padding:2px 8px;border-right:1px solid #222;">
            <div style="display:flex;justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#4ade80;">${dv}</span>
            </div>
        </td>
        <td style="padding:2px 8px;">
            <div style="display:flex;justify-content:space-between;">
                <span style="color:#555;">${label}</span>
                <span style="color:#f87171;">${cv}</span>
            </div>
        </td>
    </tr>`;
}

connection.on("TrangThaiXeUpdated", function (chuyenId, trangThai, thoiGianDen, thoiGianHoanThanh) {
    const row = document.querySelector(`tr[data-chuyen-id="${chuyenId}"]`);
    if (row) {
        const ttLabels = ["Chưa về", "Đang về", "Đã đến", "Đang nhập", "Hoàn thành"];
        row.dataset.tt = trangThai;
        row.querySelector(".cell-tt").innerHTML = `<span class="badge tt-${trangThai}">${ttLabels[trangThai]}</span>`;
        const elDen = row.querySelector(".thoigianden");
        const elXong = row.querySelector(".thoigianhoanthanh");
        if (elDen) elDen.innerText = thoiGianDen ?? "--";
        if (elXong) elXong.innerText = thoiGianHoanThanh ?? "--";
        row.classList.add("flash");
        setTimeout(() => row.classList.remove("flash"), 1500);
    }
    InvokeTongHopNhap();
});

// ==================== REALTIME ĐIỀU ĐỘ XUẤT ====================

async function reloadXeDropdown() {
    try {
        var res = await fetch('/DieuDoXuat/GetXeTrongBai');
        if (!res.ok) return;
        var xes = await res.json();
        var sel = document.getElementById('sel-xe');
        if (!sel) return;
        var curVal = sel.value;
        sel.innerHTML = '<option value="">-- Chọn xe --</option>';
        xes.forEach(function (x) {
            var id = x.id ?? x.Id;
            var bienSo = x.bienSoXe ?? x.BienSoXe ?? '--';
            var loai = x.loaiXe ?? x.LoaiXe ?? '--';
            var tai = x.taiTrong ?? x.TaiTrong ?? 0;
            var tenTaiXe = x.tenTaiXe ?? x.TenTaiXe ?? 'Chưa có tài xế';
            var tel = x.telTaiXe ?? x.TelTaiXe ?? '--';
            var opt = document.createElement('option');
            opt.value = id;
            opt.textContent = bienSo;
            opt.dataset.loai = loai;
            opt.dataset.tai = tai;
            opt.dataset.taixe = tenTaiXe;
            opt.dataset.tel = tel;
            sel.appendChild(opt);
        });
        var stillValid = Array.from(sel.options).some(function (o) { return o.value === curVal; });
        if (curVal && !stillValid) {
            sel.value = '';
            var info = document.getElementById('xe-info');
            if (info) info.classList.remove('show');
        }
    } catch (e) { console.error('reloadXeDropdown:', e); }
}

async function reloadCuaDropdown() {
    try {
        var res = await fetch('/DieuDoXuat/GetCuaTrong');
        if (!res.ok) return;
        var cuas = await res.json();
        var sel = document.getElementById('sel-cua');
        if (!sel) return;
        var curVal = sel.value;
        sel.innerHTML = '<option value="">-- Chọn cửa --</option>';
        cuas.forEach(function (c) {
            var id = c.id ?? c.Id;
            var ten = c.ten ?? c.Ten ?? '--';
            var opt = document.createElement('option');
            opt.value = id;
            opt.textContent = ten;
            sel.appendChild(opt);
        });
        var stillValid = Array.from(sel.options).some(function (o) { return o.value === curVal; });
        if (curVal && !stillValid) sel.value = '';
    } catch (e) { console.error('reloadCuaDropdown:', e); }
}

//connection.on("UpdateBangTongHopXuat", function (data) {
//    if (!data) return;
//    BindTongHopXuattoTable(data);
//    reloadXeDropdown();
//    reloadCuaDropdown();
//    // Cập nhật danh sách phiếu + sidebar cửa nếu đang ở trang DieuDoXuat
//    //if (typeof loadPhieu === 'function') {
//    //    loadPhieu();
//    //}
//});
connection.on("UpdateBangTongHopXuat", function (data) {
    if (!data) return;
    BindTongHopXuattoTable(data);
    reloadXeDropdown();
    reloadCuaDropdown();

    // Nếu đang ở màn hình cửa xuất → cập nhật ngay real-time
    var url = document.URL;
    var id = url.substring(url.lastIndexOf('/') + 1);
    if (url.indexOf("cuaxuat") !== -1 && isFinite(id) && id !== '') {
        var cuaId = parseInt(id, 10);
        InvokeXeXuat(cuaId);
        Invokexuat(cuaId);
    }
});