import { api, ApiError } from '../api.js';
import { renderHeader } from '../layout.js';
import { formatPrice } from '../format.js';

renderHeader();

const content = document.getElementById('content');
const message = document.getElementById('message');
const searchForm = document.getElementById('search-form');
const searchInput = document.getElementById('search-input');

const params = new URLSearchParams(location.search);
searchInput.value = params.get('search') ?? '';

function showError(text) {
  message.textContent = text;
  message.className = 'message message--error';
  message.hidden = false;
}

function hideMessage() {
  message.hidden = true;
}

async function load(search) {
  hideMessage();
  content.textContent = 'Yükleniyor…';

  try {
    const query = search ? `?search=${encodeURIComponent(search)}` : '';
    const result = await api(`/api/catalog/products${query}`);
    render(result.items);
  } catch (err) {
    content.textContent = '';
    if (err instanceof ApiError) showError(err.message);
    else showError('Ürünler yüklenemedi.');
  }
}

function render(products) {
  content.replaceChildren();

  if (products.length === 0) {
    const empty = document.createElement('p');
    empty.textContent = 'Ürün bulunamadı.';
    content.append(empty);
    return;
  }

  const grid = document.createElement('div');
  grid.className = 'product-grid';

  for (const product of products) {
    const card = document.createElement('a');
    card.className = 'card';
    card.href = `/product.html?id=${product.id}`;

    if (product.imageUrl) {
      const img = document.createElement('img');
      img.className = 'card__image';
      img.src = product.imageUrl;
      img.alt = product.name;
      card.append(img);
    } else {
      const placeholder = document.createElement('div');
      placeholder.className = 'card__image-placeholder';
      placeholder.textContent = 'Görsel yok';
      card.append(placeholder);
    }

    const title = document.createElement('h3');
    title.className = 'card__title';
    title.textContent = product.name;
    card.append(title);

    const price = document.createElement('span');
    price.className = 'card__price';
    price.textContent = formatPrice(product.price);
    card.append(price);

    grid.append(card);
  }

  content.append(grid);
}

searchForm.addEventListener('submit', (event) => {
  event.preventDefault();
  const search = searchInput.value.trim();
  const url = new URL(location.href);
  if (search) url.searchParams.set('search', search);
  else url.searchParams.delete('search');
  history.replaceState(null, '', url);
  load(search);
});

load(searchInput.value);
