import { api, ApiError } from '../api.js';
import { renderHeader } from '../layout.js';
import { formatPrice } from '../format.js';
import { addToCart } from '../cart.js';

renderHeader();

const content = document.getElementById('content');
const message = document.getElementById('message');

function showMessage(text, kind) {
  message.textContent = text;
  message.className = `message message--${kind}`;
  message.hidden = false;
}

function hideMessage() {
  message.hidden = true;
}

async function load() {
  const id = new URLSearchParams(location.search).get('id');
  if (!id) {
    content.textContent = '';
    showMessage('Ürün bulunamadı.', 'error');
    return;
  }

  content.textContent = 'Yükleniyor…';
  try {
    const product = await api(`/api/catalog/products/${id}`);
    render(product);
  } catch (err) {
    content.textContent = '';
    if (err instanceof ApiError && err.status === 404) showMessage('Ürün bulunamadı.', 'error');
    else showMessage('Ürün yüklenemedi.', 'error');
  }
}

function render(product) {
  hideMessage();
  content.replaceChildren();

  const wrapper = document.createElement('div');
  wrapper.className = 'product-detail';

  if (product.imageUrl) {
    const img = document.createElement('img');
    img.className = 'product-detail__image';
    img.src = product.imageUrl;
    img.alt = product.name;
    wrapper.append(img);
  } else {
    const placeholder = document.createElement('div');
    placeholder.className = 'product-detail__image-placeholder';
    placeholder.textContent = 'Görsel yok';
    wrapper.append(placeholder);
  }

  const info = document.createElement('div');
  info.className = 'product-detail__info';

  const title = document.createElement('h1');
  title.textContent = product.name;
  info.append(title);

  if (product.description) {
    const description = document.createElement('p');
    description.textContent = product.description;
    info.append(description);
  }

  const price = document.createElement('p');
  price.className = 'card__price';
  price.textContent = formatPrice(product.price);
  info.append(price);

  const stock = document.createElement('p');
  stock.textContent = product.stock > 0 ? `Stokta ${product.stock} adet var` : 'Stokta yok';
  info.append(stock);

  if (product.stock > 0) {
    const qtyLabel = document.createElement('label');
    qtyLabel.textContent = 'Adet';
    const qtyInput = document.createElement('input');
    qtyInput.type = 'number';
    qtyInput.className = 'qty-input';
    qtyInput.min = '1';
    qtyInput.max = String(product.stock);
    qtyInput.value = '1';
    qtyLabel.append(document.createElement('br'), qtyInput);
    info.append(qtyLabel);

    const addButton = document.createElement('button');
    addButton.type = 'button';
    addButton.className = 'btn';
    addButton.textContent = 'Sepete Ekle';
    addButton.addEventListener('click', () => {
      const qty = Math.max(1, Math.min(product.stock, Number(qtyInput.value) || 1));
      addToCart(product, qty);
      showMessage('Ürün sepete eklendi.', 'success');
    });
    info.append(addButton);
  }

  wrapper.append(info);
  content.append(wrapper);
}

load();
