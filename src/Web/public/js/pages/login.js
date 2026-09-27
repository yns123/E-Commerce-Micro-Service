import { api, ApiError } from '../api.js';
import { renderHeader } from '../layout.js';
import { saveSession } from '../auth.js';

renderHeader();

const form = document.getElementById('login-form');
const message = document.getElementById('message');

function showError(text) {
  message.textContent = text;
  message.className = 'message message--error';
  message.hidden = false;
}

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  message.hidden = true;

  const formData = new FormData(form);
  const email = formData.get('email');
  const password = formData.get('password');

  const submitButton = form.querySelector('button[type="submit"]');
  submitButton.disabled = true;

  try {
    const response = await api('/api/identity/login', { method: 'POST', body: { email, password } });
    saveSession(response);

    const returnUrl = new URLSearchParams(location.search).get('returnUrl');
    location.href = returnUrl || '/index.html';
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) showError('E-posta veya şifre hatalı.');
    else showError('Giriş yapılamadı.');
  } finally {
    submitButton.disabled = false;
  }
});
