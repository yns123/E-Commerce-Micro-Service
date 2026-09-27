import { api, ApiError } from '../api.js';
import { renderHeader } from '../layout.js';

renderHeader();

const form = document.getElementById('register-form');
const message = document.getElementById('message');

function showError(text) {
  message.textContent = text;
  message.className = 'message message--error';
  message.hidden = false;
}

function showSuccess(text) {
  message.textContent = text;
  message.className = 'message message--success';
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
    await api('/api/identity/register', { method: 'POST', body: { email, password } });
    showSuccess('Kayıt başarılı, giriş sayfasına yönlendiriliyorsunuz…');
    setTimeout(() => {
      location.href = `/login.html?returnUrl=${encodeURIComponent('/index.html')}`;
    }, 1200);
  } catch (err) {
    if (err instanceof ApiError && err.status === 409) showError('Bu e-posta zaten kayıtlı.');
    else if (err instanceof ApiError && err.fieldErrors) showError(Object.values(err.fieldErrors).flat().join(' '));
    else showError('Kayıt olunamadı.');
    submitButton.disabled = false;
  }
});
