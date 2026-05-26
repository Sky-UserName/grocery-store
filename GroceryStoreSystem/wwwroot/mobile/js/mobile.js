const mobileStore = {
    get token() {
        return localStorage.getItem('mobile_access_token') || '';
    },
    set token(value) {
        localStorage.setItem('mobile_access_token', value);
    },
    clear() {
        localStorage.removeItem('mobile_access_token');
    }
};

async function apiGet(url) {
    const response = await fetch(url, {
        headers: mobileStore.token ? { Authorization: `Bearer ${mobileStore.token}` } : {}
    });
    if (response.status === 401) {
        mobileStore.clear();
        location.href = '/mobile/login.html';
        return null;
    }
    if (!response.ok) throw new Error('请求失败');
    return await response.json();
}

async function getConfig() {
    const response = await fetch('/api/mobile/config');
    if (!response.ok) throw new Error('配置读取失败');
    return await response.json();
}

function setText(id, value) {
    const element = document.getElementById(id);
    if (element) element.textContent = value || '';
}

function escapeHtml(value) {
    return String(value ?? '').replace(/[&<>"']/g, char => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    })[char]);
}
