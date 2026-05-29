const pages = document.querySelectorAll(".page");
const navButtons = document.querySelectorAll(".bottom-nav button");
const richEditor = document.getElementById("richEditor");
const cardForm = document.getElementById("cardForm");
const categoryForm = document.getElementById("categoryForm");
const API_BASE_URL = window.API_BASE_URL || "http://localhost:5088";
const MOBILE_BASE_URL = window.MOBILE_BASE_URL || "http://localhost:5100/login.html";

let state = {
    config: {},
    categories: [],
    cards: [],
    stats: { categoryCount: 0, cardCount: 0, publishedCount: 0 },
    signedIn: false
};

function mobileUrl() {
    return MOBILE_BASE_URL;
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

async function api(url, options = {}) {
    const response = await fetch(apiUrl(url), {
        credentials: "include",
        headers: options.body instanceof FormData ? {} : { "Content-Type": "application/json" },
        ...options
    });
    if (response.status === 401) {
        showLogin(true);
        throw new Error("请先登录");
    }
    if (!response.ok) {
        let message = "请求失败";
        try {
            const data = await response.json();
            message = data.message || data.Message || message;
        } catch {}
        throw new Error(message);
    }
    const type = response.headers.get("content-type") || "";
    return type.includes("application/json") ? response.json() : response;
}

function switchPage(name) {
    pages.forEach(page => page.classList.toggle("active", page.dataset.page === name));
    navButtons.forEach(button => button.classList.toggle("active", button.dataset.nav === name));
    render();
}

function showLogin(visible) {
    document.getElementById("loginModal").classList.toggle("active", visible);
}

async function loadState() {
    const session = await api("/api/admin/session");
    state.signedIn = session.authenticated;
    if (!state.signedIn) {
        showLogin(true);
        return;
    }
    showLogin(false);
    const [stats, config, categories, cards] = await Promise.all([
        api("/api/admin/dashboard"),
        api("/api/admin/config"),
        api("/api/admin/categories"),
        api("/api/admin/cards")
    ]);
    state.stats = stats;
    state.config = config;
    state.categories = categories;
    state.cards = cards;
    render();
}

function render() {
    renderHome();
    renderConfig();
    renderCategories();
    renderCards();
    renderCardCategoryOptions();
}

function renderHome() {
    document.getElementById("categoryCount").textContent = state.stats.categoryCount || 0;
    document.getElementById("cardCount").textContent = state.stats.cardCount || 0;
    document.getElementById("publishedCount").textContent = state.stats.publishedCount || 0;
    const link = document.getElementById("mobileLink");
    link.href = mobileUrl();
    link.textContent = mobileUrl();
    document.getElementById("qrText").value ||= mobileUrl();
}

function renderConfig() {
    const form = document.getElementById("configForm");
    form.siteName.value = state.config.siteName || "";
    form.subtitle.value = state.config.siteSubtitle || "";
    form.description.value = state.config.siteDescription || "";
    form.accessPassword.value = "";
    form.currentPassword.value = "";
    form.newAdminPassword.value = "";
    form.confirmAdminPassword.value = "";
    form.passwordEnabled.checked = Boolean(state.config.accessPasswordEnabled);
}

function renderCategories() {
    const list = document.getElementById("categoryList");
    const items = [...state.categories].sort((a, b) => a.sortOrder - b.sortOrder);
    list.innerHTML = items.length ? items.map(category => `
        <article class="list-item">
            <div class="list-main">
                <strong>${escapeHtml(category.name)}</strong>
                <small>${escapeHtml(category.slug)} · 排序 ${category.sortOrder}</small>
                <div class="actions"><span class="pill">${category.isEnabled ? "启用" : "禁用"}</span></div>
            </div>
            <button class="btn ghost" data-edit-category="${category.id}">编辑</button>
            <button class="btn danger" data-delete-category="${category.id}">删</button>
        </article>
    `).join("") : `<div class="empty">暂无分类</div>`;
}

function renderCards() {
    const list = document.getElementById("cardList");
    const items = [...state.cards].sort((a, b) => a.sortOrder - b.sortOrder);
    list.innerHTML = items.length ? items.map(card => `
        <article class="list-item">
            <img src="${mediaUrl(card.coverImageUrl)}" alt="">
            <div class="list-main">
                <strong>${escapeHtml(card.title)}</strong>
                <small>${escapeHtml(card.categoryName || "未分类")} · ${escapeHtml(card.summary)}</small>
                <div class="actions">
                    <span class="pill">${escapeHtml(card.statusText)}</span>
                    ${(card.tags || []).map(tag => `<span class="pill">${escapeHtml(tag)}</span>`).join("")}
                </div>
            </div>
            <button class="btn ghost" data-edit-card="${card.id}">编辑</button>
            <button class="btn danger" data-delete-card="${card.id}">删</button>
        </article>
    `).join("") : `<div class="empty">暂无卡片</div>`;
}

function renderCardCategoryOptions() {
    cardForm.categoryId.innerHTML = state.categories.map(category => `<option value="${category.id}">${escapeHtml(category.name)}</option>`).join("");
}

function openModal(id) {
    document.getElementById(id).classList.add("active");
}

function closeModal(id) {
    document.getElementById(id).classList.remove("active");
}

function editCategory(id) {
    const category = id ? state.categories.find(item => String(item.id) === String(id)) : null;
    categoryForm.id.value = category?.id || 0;
    categoryForm.name.value = category?.name || "";
    categoryForm.slug.value = category?.slug || "";
    categoryForm.sort.value = category?.sortOrder || ((state.categories.length + 1) * 10);
    categoryForm.enabled.checked = category?.isEnabled ?? true;
    openModal("categoryModal");
}

async function editCard(id) {
    const card = id ? await api(`/api/admin/cards/${id}`) : null;
    cardForm.id.value = card?.id || 0;
    cardForm.categoryId.value = card?.categoryId || state.categories[0]?.id || "";
    cardForm.status.value = card?.status ?? 0;
    cardForm.title.value = card?.title || "";
    cardForm.summary.value = card?.summary || "";
    cardForm.sort.value = card?.sortOrder || ((state.cards.length + 1) * 10);
    cardForm.tags.value = (card?.tags || []).join(",");
    cardForm.cover.value = card?.coverImageUrl || "";
    document.getElementById("coverPreview").src = mediaUrl(card?.coverImageUrl);
    richEditor.innerHTML = card?.contentHtml ? rewriteMediaUrls(card.contentHtml) : "<p>请输入详情内容。</p>";
    openModal("cardModal");
}

async function uploadImage(file, usageType) {
    const data = new FormData();
    data.append("file", file);
    data.append("usageType", usageType);
    return api("/api/admin/uploads/image", { method: "POST", body: data });
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

document.querySelectorAll("[data-nav]").forEach(button => button.addEventListener("click", () => switchPage(button.dataset.nav)));
document.querySelectorAll("[data-go]").forEach(button => button.addEventListener("click", () => switchPage(button.dataset.go)));
document.querySelectorAll("[data-close]").forEach(button => button.addEventListener("click", () => closeModal(button.dataset.close)));
document.getElementById("newCategory").addEventListener("click", () => editCategory());
document.getElementById("newCard").addEventListener("click", () => editCard());

document.getElementById("loginForm").addEventListener("submit", async event => {
    event.preventDefault();
    try {
        await api("/api/admin/login", {
            method: "POST",
            body: JSON.stringify({ username: event.target.username.value, password: event.target.password.value })
        });
        await loadState();
    } catch (error) {
        alert(error.message);
    }
});

document.getElementById("configForm").addEventListener("submit", async event => {
    event.preventDefault();
    try {
        const currentPassword = event.target.currentPassword.value.trim();
        const newPassword = event.target.newAdminPassword.value.trim();
        const confirmPassword = event.target.confirmAdminPassword.value.trim();
        const shouldChangePassword = Boolean(currentPassword || newPassword || confirmPassword);

        if (shouldChangePassword && (!currentPassword || !newPassword || !confirmPassword)) {
            throw new Error("请完整填写原密码、新密码和确认密码");
        }

        if (shouldChangePassword && newPassword !== confirmPassword) {
            throw new Error("两次输入的新密码不一致");
        }

        await api("/api/admin/config", {
            method: "PUT",
            body: JSON.stringify({
                siteName: event.target.siteName.value.trim(),
                siteSubtitle: event.target.subtitle.value.trim(),
                siteDescription: event.target.description.value.trim(),
                accessPassword: event.target.accessPassword.value.trim() || null,
                accessPasswordEnabled: event.target.passwordEnabled.checked
            })
        });

        if (shouldChangePassword) {
            await api("/api/admin/change-password", {
                method: "POST",
                body: JSON.stringify({
                    currentPassword,
                    newPassword,
                    confirmPassword
                })
            });
        }

        alert(shouldChangePassword ? "配置和后台密码已保存" : "配置已保存");
        await loadState();
    } catch (error) {
        alert(error.message);
    }
});

document.body.addEventListener("click", async event => {
    const editCategoryId = event.target.dataset.editCategory;
    const deleteCategoryId = event.target.dataset.deleteCategory;
    const editCardId = event.target.dataset.editCard;
    const deleteCardId = event.target.dataset.deleteCard;

    try {
        if (editCategoryId) editCategory(editCategoryId);
        if (editCardId) await editCard(editCardId);
        if (deleteCategoryId && confirm("确认删除分类？")) {
            await api(`/api/admin/categories/${deleteCategoryId}`, { method: "DELETE" });
            await loadState();
        }
        if (deleteCardId && confirm("确认删除卡片？")) {
            await api(`/api/admin/cards/${deleteCardId}`, { method: "DELETE" });
            await loadState();
        }
    } catch (error) {
        alert(error.message);
    }
});

categoryForm.addEventListener("submit", async event => {
    event.preventDefault();
    try {
        await api("/api/admin/categories", {
            method: "POST",
            body: JSON.stringify({
                id: Number(categoryForm.id.value || 0),
                name: categoryForm.name.value.trim(),
                slug: categoryForm.slug.value.trim(),
                sortOrder: Number(categoryForm.sort.value || 0),
                isEnabled: categoryForm.enabled.checked
            })
        });
        closeModal("categoryModal");
        await loadState();
    } catch (error) {
        alert(error.message);
    }
});

cardForm.addEventListener("submit", async event => {
    event.preventDefault();
    try {
        const result = await api("/api/admin/cards", {
            method: "POST",
            body: JSON.stringify({
                id: Number(cardForm.id.value || 0),
                categoryId: Number(cardForm.categoryId.value),
                status: Number(cardForm.status.value),
                title: cardForm.title.value.trim(),
                summary: cardForm.summary.value.trim(),
                sortOrder: Number(cardForm.sort.value || 0),
                tags: cardForm.tags.value.split(/[,，]/).map(tag => tag.trim()).filter(Boolean),
                coverImageUrl: cardForm.cover.value || null,
                contentHtml: richEditor.innerHTML
            })
        });
        cardForm.id.value = result.id;
        closeModal("cardModal");
        await loadState();
    } catch (error) {
        alert(error.message);
    }
});

document.querySelectorAll("[data-command]").forEach(button => {
    button.addEventListener("click", () => {
        document.execCommand(button.dataset.command, false);
        richEditor.focus();
    });
});

document.getElementById("insertLink").addEventListener("click", () => {
    const url = prompt("请输入链接");
    if (url) document.execCommand("createLink", false, url);
});

document.getElementById("coverFile").addEventListener("change", async event => {
    const file = event.target.files[0];
    if (!file) return;
    try {
        const result = await uploadImage(file, "cover");
        cardForm.cover.value = result.url;
        document.getElementById("coverPreview").src = mediaUrl(result.url);
    } catch (error) {
        alert(error.message);
    }
});

document.getElementById("contentImage").addEventListener("change", async event => {
    const file = event.target.files[0];
    if (!file) return;
    try {
        const result = await uploadImage(file, "content");
        document.execCommand("insertHTML", false, `<figure><img src="${mediaUrl(result.url)}" alt=""><figcaption>图片说明</figcaption></figure>`);
        event.target.value = "";
    } catch (error) {
        alert(error.message);
    }
});

document.getElementById("makeQr").addEventListener("click", () => {
    const text = document.getElementById("qrText").value.trim();
    if (!text) return;
    document.getElementById("qrBox").innerHTML = `<img src="${apiUrl(`/api/admin/qrcode/image?url=${encodeURIComponent(text)}`)}" alt="二维码">`;
});

document.getElementById("resetDemo").addEventListener("click", () => {
    alert("演示数据来自数据库，如需重置请清空数据库后重启服务。");
});

loadState();
