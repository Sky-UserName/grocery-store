const API_BASE_URL = window.API_BASE_URL || "http://localhost:5083";
const loginForm = document.getElementById("loginForm");
const loginError = document.getElementById("loginError");

function apiUrl(path) {
    return new URL(path, API_BASE_URL).href;
}

function getNextUrl() {
    const next = new URLSearchParams(location.search).get("next") || "index.html";

    try {
        const target = new URL(next, location.href);
        if (target.origin === location.origin && !target.pathname.endsWith("/login.html")) {
            return target.href;
        }
    } catch {}

    return "index.html";
}

async function requestJson(path, options = {}) {
    const response = await fetch(apiUrl(path), {
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        ...options
    });

    if (!response.ok) {
        let message = "请求失败";
        try {
            const data = await response.json();
            message = data.message || data.Message || message;
        } catch {}
        throw new Error(message);
    }

    return response.json();
}

async function redirectIfSignedIn() {
    const session = await requestJson("/api/admin/session");
    if (session.authenticated) {
        location.href = getNextUrl();
    }
}

loginForm.addEventListener("submit", async event => {
    event.preventDefault();
    const submitButton = loginForm.querySelector("button[type='submit']");
    loginError.textContent = "";
    submitButton.disabled = true;

    try {
        await requestJson("/api/admin/login", {
            method: "POST",
            body: JSON.stringify({
                username: loginForm.username.value.trim(),
                password: loginForm.password.value
            })
        });
        location.href = getNextUrl();
    } catch (error) {
        loginError.textContent = error.message;
    } finally {
        submitButton.disabled = false;
    }
});

redirectIfSignedIn().catch(() => {});
