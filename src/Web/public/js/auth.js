const TOKEN_KEY = 'token';
const USER_KEY = 'user';

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function getUser() {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function isLoggedIn() {
  const user = getUser();
  if (!user || !getToken()) return false;
  return new Date(user.expiresAt) > new Date();
}

export function isAdmin() {
  return isLoggedIn() && getUser()?.role === 'Admin';
}

export function saveSession(loginResponse) {
  localStorage.setItem(TOKEN_KEY, loginResponse.token);
  localStorage.setItem(USER_KEY, JSON.stringify({
    email: loginResponse.email,
    role: loginResponse.role,
    expiresAt: loginResponse.expiresAt,
  }));
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export function logout() {
  clearSession();
  location.href = '/index.html';
}

export function requireLogin() {
  if (!isLoggedIn()) {
    const returnUrl = encodeURIComponent(location.pathname + location.search);
    location.href = `/login.html?returnUrl=${returnUrl}`;
  }
}

export function requireAdmin() {
  if (!isAdmin()) {
    location.href = '/index.html';
  }
}
