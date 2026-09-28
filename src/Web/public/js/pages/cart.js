import { api, ApiError } from '../api.js';
import { renderHeader } from '../layout.js';
import { formatPrice } from '../format.js';
import { getCart, updateQuantity, removeFromCart, clearCart } from '../cart.js';
import { isLoggedIn } from '../auth.js';

renderHeader();

const content = document.getElementById('content');
const message = document.getElementById('message');

function showError(text) {
  message.textContent = text;
  message.className = 'message message--error';
  message.hidden = false;
}

function hideMessage() {
  message.hidden = true;
}

function render() {
  hideMessage();
  const cart = getCart();
  content.replaceChildren();

  if (cart.length === 0) {
    const empty = document.createElement('p');
    empty.textContent = 'Sepetiniz boş.';
    content.append(empty);
    return;
  }

  let total = 0;

  for (const item of cart) {
    total += item.price * item.quantity;

    const row = document.createElement('div');
    row.className = 'cart-row';

    const name = document.createElement('span');
    name.className = 'cart-row__name';
    name.textContent = item.name;
    row.append(name);

    const qtyInput = document.createElement('input');
    qtyInput.type = 'number';
    qtyInput.className = 'qty-input';
    qtyInput.min = '1';
    qtyInput.value = String(item.quantity);
    qtyInput.addEventListener('change', () => {
      updateQuantity(item.productId, Number(qtyInput.value) || 1);
      render();
    });
    row.append(qtyInput);

    const price = document.createElement('span');
    price.textContent = formatPrice(item.price * item.quantity);
    row.append(price);

    const removeButton = document.createElement('button');
    removeButton.type = 'button';
    removeButton.className = 'btn btn--danger';
    removeButton.textContent = 'Kaldır';
    removeButton.addEventListener('click', () => {
      removeFromCart(item.productId);
      render();
    });
    row.append(removeButton);

    content.append(row);
  }

  const totalRow = document.createElement('div');
  totalRow.className = 'cart-total';
  totalRow.textContent = `Toplam: ${formatPrice(total)}`;
  content.append(totalRow);

  const actions = document.createElement('div');
  actions.className = 'cart-actions';

  const orderButton = document.createElement('button');
  orderButton.type = 'button';
  orderButton.className = 'btn';
  orderButton.textContent = 'Siparişi Ver';
  orderButton.addEventListener('click', () => submitOrder(cart, orderButton));
  actions.append(orderButton);

  content.append(actions);
}

async function submitOrder(cart, button) {
  if (!isLoggedIn()) {
    location.href = `/login.html?returnUrl=${encodeURIComponent('/cart.html')}`;
    return;
  }

  hideMessage();
  button.disabled = true;

  try {
    const items = cart.map((item) => ({ productId: item.productId, quantity: item.quantity }));
    const response = await api('/api/ordering/orders', { method: 'POST', body: { items } });
    clearCart();
    location.href = `/orders.html?highlight=${response.orderId}`;
  } catch (err) {
    button.disabled = false;
    if (err instanceof ApiError) showError(err.message);
    else showError('Sipariş verilemedi.');
  }
}

render();
