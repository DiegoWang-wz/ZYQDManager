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

