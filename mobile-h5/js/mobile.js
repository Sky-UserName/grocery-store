function getCurrentMobilePage() {
    return location.pathname.split("/").pop() || "index.html";
}

(function guardWeChatBrowser() {
    const guidePage = "open-browser.html";
    const currentPage = getCurrentMobilePage();
    const isWeChat = /MicroMessenger/i.test(navigator.userAgent);

    if (isWeChat && currentPage !== guidePage) {
        const redirectPath = `${location.pathname}${location.search}${location.hash}`;
        window.__WECHAT_BROWSER_GUIDE_ACTIVE__ = true;
        location.replace(`${guidePage}?redirect=${encodeURIComponent(redirectPath)}`);
        return;
    }

    if (!isWeChat && currentPage === guidePage) {
        const redirect = new URLSearchParams(location.search).get("redirect");
        if (!redirect) return;

        try {
            const target = new URL(redirect, location.href);
            if (target.origin === location.origin && !target.pathname.endsWith(`/${guidePage}`)) {
                location.replace(target.href);
            }
        } catch {
            // Ignore malformed redirect values and leave the guide visible.
        }
    }
})();

const API_BASE_URL = window.API_BASE_URL || "http://localhost:5088";

const mobileStore = {
    get token() {
        return localStorage.getItem("mobile_access_token") || "";
    },
    set token(value) {
        localStorage.setItem("mobile_access_token", value);
    },
    clear() {
        localStorage.removeItem("mobile_access_token");
    }
};

async function apiGet(url) {
    const response = await fetch(apiUrl(url), {
        headers: mobileStore.token ? { Authorization: `Bearer ${mobileStore.token}` } : {}
    });
    if (response.status === 401) {
        mobileStore.clear();
        location.href = "login.html";
        return null;
    }
    if (!response.ok) throw new Error("请求失败");
    return response.json();
}

async function getConfig() {
    const response = await fetch(apiUrl("/api/mobile/config"));
    if (!response.ok) throw new Error("配置读取失败");
    return response.json();
}

function setText(id, value) {
    const element = document.getElementById(id);
    if (element) element.textContent = value || "";
}

function apiUrl(path) {
    return new URL(path, API_BASE_URL).href;
}

function mediaUrl(path) {
    if (!path) return "";
    return /^https?:\/\//i.test(path) ? path : apiUrl(path);
}

function rewriteMediaUrls(html) {
    const wrapper = document.createElement("div");
    wrapper.innerHTML = html;
    wrapper.querySelectorAll("img[src]").forEach(img => {
        img.src = mediaUrl(img.getAttribute("src"));
    });
    return wrapper.innerHTML;
}

function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, char => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        "\"": "&quot;",
        "'": "&#39;"
    })[char]);
}
