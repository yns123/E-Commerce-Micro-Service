import { getToken, clearSession } from './auth.js';

export class ApiError extends Error {
  constructor(status, message, fieldErrors) {
    super(message);
    this.status = status;
    this.fieldErrors = fieldErrors ?? null;
  }
}

export async function api(path, { method = 'GET', body } = {}) {
  const token = getToken();
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const response = await fetch(path, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  const text = await response.text();
  let data = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = null;
    }
  }

  if (response.status === 401 && token) {
    clearSession();
    const returnUrl = encodeURIComponent(location.pathname + location.search);
    location.href = `/login.html?returnUrl=${returnUrl}`;
    throw new ApiError(401, 'Oturumunuzun süresi doldu, tekrar giriş yapın.');
  }

  if (!response.ok) {
    const message = data?.title || data?.detail || 'Bir hata oluştu.';
    throw new ApiError(response.status, message, data?.errors ?? null);
  }

  if (response.status === 204) return null;
  return data;
}
