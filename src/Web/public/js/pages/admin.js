import { api, ApiError } from '../api.js';
import { renderHeader } from '../layout.js';
import { requireAdmin } from '../auth.js';
import { formatPrice } from '../format.js';

renderHeader();
requireAdmin();

const content = document.getElementById('content');
const message = document.getElementById('message');
const createForm = document.getElementById('create-form');
const deleteDialog = document.getElementById('delete-dialog');

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

function hideMessage() {
  message.hidden = true;
}

async function loadProducts() {
  content.textContent = 'Yükleniyor…';
  try {
    const result = await api('/api/catalog/products?pageSize=100');
    renderProducts(result.items);
  } catch {
    content.textContent = '';
    showError('Ürünler yüklenemedi.');
  }
}

function renderProducts(products) {
  content.replaceChildren();

  if (products.length === 0) {
    const empty = document.createElement('p');
    empty.textContent = 'Ürün yok.';
    content.append(empty);
    return;
  }

  for (const product of products) {
    const row = document.createElement('div');
    row.className = 'admin-row';
    renderViewMode(row, product);
    content.append(row);
  }
}

function renderViewMode(row, product) {
  row.replaceChildren();

  const name = document.createElement('span');
  name.className = 'admin-row__name';
  name.textContent = product.name;
  row.append(name);

  const price = document.createElement('span');
  price.textContent = formatPrice(product.price);
  row.append(price);

  const stock = document.createElement('span');
  stock.textContent = `Stok: ${product.stock}`;
  row.append(stock);

  const editButton = document.createElement('button');
  editButton.type = 'button';
  editButton.className = 'btn btn--secondary';
  editButton.textContent = 'Düzenle';
  editButton.addEventListener('click', () => renderEditMode(row, product));
  row.append(editButton);

  const deleteButton = document.createElement('button');
  deleteButton.type = 'button';
  deleteButton.className = 'btn btn--danger';
  deleteButton.textContent = 'Sil';
  deleteButton.addEventListener('click', () => confirmDelete(product));
  row.append(deleteButton);
}

function renderEditMode(row, product) {
  row.replaceChildren();

  const form = document.createElement('form');
  form.className = 'admin-row__edit-form';

  const nameInput = document.createElement('input');
  nameInput.type = 'text';
  nameInput.required = true;
  nameInput.maxLength = 200;
  nameInput.value = product.name;
  form.append(nameInput);

  const descInput = document.createElement('input');
  descInput.type = 'text';
  descInput.maxLength = 2000;
  descInput.value = product.description ?? '';
  form.append(descInput);

  const priceInput = document.createElement('input');
  priceInput.type = 'number';
  priceInput.min = '0.01';
  priceInput.step = '0.01';
  priceInput.required = true;
  priceInput.value = String(product.price);
  form.append(priceInput);

  const stockInput = document.createElement('input');
  stockInput.type = 'number';
  stockInput.min = '0';
  stockInput.step = '1';
  stockInput.required = true;
  stockInput.value = String(product.stock);
  form.append(stockInput);

  const imageInput = document.createElement('input');
  imageInput.type = 'url';
  imageInput.value = product.imageUrl ?? '';
  form.append(imageInput);

  const saveButton = document.createElement('button');
  saveButton.type = 'submit';
  saveButton.className = 'btn';
  saveButton.textContent = 'Kaydet';
  form.append(saveButton);

  const cancelButton = document.createElement('button');
  cancelButton.type = 'button';
  cancelButton.className = 'btn btn--secondary';
  cancelButton.textContent = 'İptal';
  cancelButton.addEventListener('click', () => renderViewMode(row, product));
  form.append(cancelButton);

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    hideMessage();

    const updated = {
      name: nameInput.value.trim(),
      description: descInput.value.trim() || null,
      price: Number(priceInput.value),
      stock: Number(stockInput.value),
      imageUrl: imageInput.value.trim() || null,
    };

    try {
      await api(`/api/catalog/products/${product.id}`, { method: 'PUT', body: updated });
      showSuccess('Ürün güncellendi.');
      await loadProducts();
    } catch (err) {
      if (err instanceof ApiError) showError(err.message);
      else showError('Güncellenemedi.');
    }
  });

  row.append(form);
}

function confirmDelete(product) {
  deleteDialog.showModal();
  deleteDialog.addEventListener('close', async function onClose() {
    deleteDialog.removeEventListener('close', onClose);
    if (deleteDialog.returnValue !== 'confirm') return;

    hideMessage();
    try {
      await api(`/api/catalog/products/${product.id}`, { method: 'DELETE' });
      showSuccess('Ürün silindi.');
      await loadProducts();
    } catch (err) {
      if (err instanceof ApiError) showError(err.message);
      else showError('Silinemedi.');
    }
  });
}

createForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  hideMessage();

  const formData = new FormData(createForm);
  const request = {
    name: formData.get('name').trim(),
    description: formData.get('description')?.trim() || null,
    price: Number(formData.get('price')),
    stock: Number(formData.get('stock')),
    imageUrl: formData.get('imageUrl')?.trim() || null,
  };

  const submitButton = createForm.querySelector('button[type="submit"]');
  submitButton.disabled = true;

  try {
    await api('/api/catalog/products', { method: 'POST', body: request });
    createForm.reset();
    showSuccess('Ürün eklendi.');
    await loadProducts();
  } catch (err) {
    if (err instanceof ApiError) showError(err.message);
    else showError('Ürün eklenemedi.');
  } finally {
    submitButton.disabled = false;
  }
});

loadProducts();
