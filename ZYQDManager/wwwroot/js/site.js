window.cpiOpenActiveDatePicker = function () {
    const el = document.activeElement;
    if (!el || el.type !== "date" || typeof el.showPicker !== "function") return;
    try { el.showPicker(); } catch (_) {}
};

document.addEventListener("keydown", function (e) {
    if (e.key !== "Enter" && e.key !== "NumpadEnter") return;
    const t = e.target;
    if (!(t instanceof HTMLInputElement)) return;
    if (!t.classList.contains("cpi-lookup")) return;
    e.preventDefault();
}, true);

window.downloadFileFromBase64 = function (fileName, contentType, base64) {
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const blob = new Blob([bytes], { type: contentType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
};

window.openPdfFromBase64 = function (base64) {
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const blob = new Blob([bytes], { type: "application/pdf" });
    const url = URL.createObjectURL(blob);
    window.open(url, "_blank");
    setTimeout(function () { URL.revokeObjectURL(url); }, 60_000);
};

window.cpiUnfreezeHeader = function (wrapSel) {
    const wrap = document.querySelector(wrapSel);
    if (!wrap) return;
    const thead = wrap.querySelector("thead");
    if (thead) {
        thead.style.position = "";
        thead.style.top = "";
        thead.style.zIndex = "";
        thead.style.background = "";
        thead.style.boxShadow = "";
    }
    wrap.querySelectorAll("thead th, thead td, .cpi-stick th, .cpi-stick td").forEach(function (cell) {
        cell.style.position = "";
        cell.style.top = "";
        cell.style.zIndex = "";
        cell.style.background = "";
        cell.style.backgroundClip = "";
        cell.style.boxShadow = "";
    });
};

window.cpiFreezeHeader = function (wrapSel) {
    const wrap = document.querySelector(wrapSel);
    if (!wrap) return;
    window.cpiUnfreezeHeader(wrapSel);
    const thead = wrap.querySelector("thead");
    if (thead) {
        thead.style.position = "sticky";
        thead.style.top = "0px";
        thead.style.zIndex = "30";
        thead.style.background = "#fff";
        thead.style.boxShadow = "0 1px 0 #000";
        thead.querySelectorAll("th, td").forEach(function (cell) {
            cell.style.background = "#fff";
            cell.style.backgroundClip = "padding-box";
        });
        return;
    }
    const rows = wrap.querySelectorAll("tr.cpi-stick");
    let top = 0;
    rows.forEach(function (tr, i) {
        const height = tr.getBoundingClientRect().height;
        if (!height) return;
        const last = i === rows.length - 1;
        tr.querySelectorAll("th, td").forEach(function (cell) {
            cell.style.position = "sticky";
            cell.style.top = top + "px";
            cell.style.zIndex = String(30 + i);
            cell.style.background = "#fff";
            cell.style.backgroundClip = "padding-box";
            if (last)
                cell.style.boxShadow = "0 1px 0 #000";
        });
        top += height;
    });
};

/** SIR 明细表头：图例 + 双行标题整块吸顶，按行实测高度堆叠并重叠 2px 封缝 */
window.sirFreezeDetailHeader = function () {
    const table = document.querySelector(".sir-detail-table");
    if (!table) return false;
    const rows = Array.prototype.slice.call(table.querySelectorAll("thead tr"));
    if (!rows.length) return false;

    let top = 0;
    let ok = false;
    rows.forEach(function (tr, i) {
        // 用非 rowspan 单元格量行高，避免 rowspan 撑高测量
        const cells = Array.prototype.slice.call(tr.querySelectorAll("th, td"));
        const measure = cells.find(function (c) {
            const rs = parseInt(c.getAttribute("rowspan") || "1", 10);
            return rs <= 1;
        }) || cells[0];
        if (!measure) return;
        const h = Math.ceil(measure.getBoundingClientRect().height);
        if (!h) return;
        ok = true;

        const last = i === rows.length - 1;
        const stickTop = i === 0 ? 0 : Math.max(0, top - 2);

        cells.forEach(function (cell) {
            const rs = parseInt(cell.getAttribute("rowspan") || "1", 10);
            const isLegend = cell.classList.contains("sir-legend-cell")
                || cell.classList.contains("sir-legend-spacer");
            const bg = isLegend ? "#fff" : "#f3f4f6";

            cell.style.position = "sticky";
            cell.style.top = stickTop + "px";
            cell.style.zIndex = String(40 + i + (rs > 1 ? 12 : 0));
            cell.style.background = bg;
            cell.style.backgroundClip = "padding-box";

            if (last && rs <= 1) {
                cell.style.boxShadow = "0 -2px 0 0 " + bg + ", 0 1px 0 0 #111";
            } else if (rs > 1) {
                cell.style.boxShadow = "0 -8px 0 0 #fff, 0 2px 0 0 " + bg;
            } else {
                cell.style.boxShadow = "0 -2px 0 0 " + bg + ", 0 2px 0 0 " + bg;
            }
        });

        top += h;
    });
    return ok;
};

window.sirFreezeDetailHeaderSoon = function () {
    const tryOnce = function () {
        if (window.sirFreezeDetailHeader()) return;
        requestAnimationFrame(function () {
            window.sirFreezeDetailHeader();
        });
    };
    tryOnce();
    setTimeout(tryOnce, 50);
    setTimeout(tryOnce, 200);
    setTimeout(tryOnce, 500);
};

