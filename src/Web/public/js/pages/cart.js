import { renderHeader } from '../layout.js';
import { formatPrice } from '../format.js';
import { getCart, updateQuantity, removeFromCart } from '../cart.js';

renderHeader();

const content = document.getElementById('content');

function render() {
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
}

render();
