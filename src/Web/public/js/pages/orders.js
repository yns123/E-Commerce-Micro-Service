import { api, ApiError } from '../api.js';
import { renderHeader } from '../layout.js';
import { requireLogin } from '../auth.js';
import { formatPrice, formatDate } from '../format.js';

renderHeader();
requireLogin();

const content = document.getElementById('content');
const message = document.getElementById('message');
const highlightId = new URLSearchParams(location.search).get('highlight');

const pollStart = Date.now();
const POLL_INTERVAL_MS = 2000;
const POLL_TIMEOUT_MS = 30000;

function showError(text) {
  message.textContent = text;
  message.className = 'message message--error';
  message.hidden = false;
}

function statusLabel(status) {
  if (status === 'Pending') return 'Bekliyor';
  if (status === 'Confirmed') return 'Onaylandı';
  if (status === 'Cancelled') return 'İptal Edildi';
  return status;
}

function statusBadgeClass(status) {
  if (status === 'Pending') return 'status-badge status-badge--pending';
  if (status === 'Confirmed') return 'status-badge status-badge--confirmed';
  if (status === 'Cancelled') return 'status-badge status-badge--cancelled';
  return 'status-badge';
}

async function load() {
  try {
    const result = await api('/api/ordering/orders?pageSize=50');
    render(result.items);

    const hasPending = result.items.some((order) => order.status === 'Pending');
    const elapsed = Date.now() - pollStart;

    if (hasPending && elapsed < POLL_TIMEOUT_MS) {
      setTimeout(load, POLL_INTERVAL_MS);
    }
  } catch (err) {
    content.textContent = '';
    if (err instanceof ApiError) showError(err.message);
    else showError('Siparişler yüklenemedi.');
  }
}

function render(orders) {
  content.replaceChildren();

  if (orders.length === 0) {
    const empty = document.createElement('p');
    empty.textContent = 'Henüz siparişiniz yok.';
    content.append(empty);
    return;
  }

  for (const order of orders) {
    const card = document.createElement('div');
    card.className = 'card order-card';
    if (order.id === highlightId) card.classList.add('order-card--highlight');

    const header = document.createElement('div');
    header.className = 'order-card__header';

    const idLabel = document.createElement('strong');
    idLabel.textContent = `Sipariş #${order.id.slice(0, 8)}`;
    header.append(idLabel);

    const badge = document.createElement('span');
    badge.className = statusBadgeClass(order.status);
    badge.textContent = statusLabel(order.status);
    header.append(badge);

    card.append(header);

    const date = document.createElement('p');
    date.className = 'order-card__date';
    date.textContent = formatDate(order.createdAt);
    card.append(date);

    if (order.cancelReason) {
      const reason = document.createElement('p');
      reason.className = 'message message--error';
      reason.textContent = order.cancelReason;
      card.append(reason);
    }

    const itemsList = document.createElement('ul');
    itemsList.className = 'order-card__items';
    for (const item of order.items) {
      const li = document.createElement('li');
      const name = item.productName ?? 'Ürün';
      const priceText = item.unitPrice != null ? ` — ${formatPrice(item.unitPrice)}` : '';
      li.textContent = `${name} × ${item.quantity}${priceText}`;
      itemsList.append(li);
    }
    card.append(itemsList);

    const total = document.createElement('p');
    total.className = 'card__price';
    total.textContent = `Toplam: ${formatPrice(order.total)}`;
    card.append(total);

    content.append(card);
  }
}

load();
