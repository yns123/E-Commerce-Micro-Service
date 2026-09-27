---
name: frontend-page
description: src/Web altında yeni bir HTML sayfası eklerken veya mevcut bir sayfayı (ürün listesi, sepet, giriş, siparişler, admin paneli vb.) değiştirirken kullan. HTML, CSS veya vanilla JavaScript yazılan her frontend işinde bu kuralları takip et — özellikle bir kütüphane ya da framework eklemek aklına geldiğinde.
---

# Frontend Sayfası Ekleme

Kaynak kurallar: docs/frontend.md. Özet: saf HTML/CSS/JS, kütüphane yok, build yok. Bir şey kütüphanesiz zor görünüyorsa önce tarayıcının yerleşik API'lerine bak (`fetch`, `URLSearchParams`, `Intl`, `<template>`, `<dialog>`, `FormData`).

## 1. HTML iskeleti — public/<sayfa>.html
```html
<!doctype html>
<html lang="tr">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Sepet — Mağaza</title>
  <link rel="stylesheet" href="css/style.css">
</head>
<body>
  <header id="site-header"></header>
  <main class="container">
    <h1>Sepet</h1>
    <div id="message" class="message" hidden></div>
    <div id="content"></div>
  </main>
  <script type="module" src="js/pages/cart.js"></script>
</body>
</html>
```

## 2. Sayfa scripti — js/pages/<sayfa>.js
```js
import { api } from '../api.js';
import { renderHeader } from '../layout.js';
import { requireLogin } from '../auth.js';
import { formatPrice } from '../format.js';

renderHeader();
requireLogin();                         // sadece giriş gerektiren sayfalarda

const content = document.getElementById('content');
const message = document.getElementById('message');

function showError(text) {
  message.textContent = text;
  message.className = 'message message--error';
  message.hidden = false;
}

async function load() {
  content.textContent = 'Yükleniyor…';
  try {
    const orders = await api('/api/ordering/orders');
    render(orders.items);
  } catch (err) {
    showError(err.message);
  }
}

function render(items) {
  content.replaceChildren();
  for (const item of items) {
    const row = document.createElement('div');
    row.className = 'card';
    const title = document.createElement('h3');
    title.textContent = item.productName;      // veriyi HER ZAMAN textContent ile yaz
    row.append(title);
    content.append(row);
  }
}

load();
```

## 3. Kontrol listesi
- [ ] Hiçbir `<script src="http...">`, CDN, npm paketi yok
- [ ] API çağrıları sadece `api()` ile, göreli yollarla (`/api/...`)
- [ ] API/kullanıcı verisi `innerHTML` ile basılmıyor
- [ ] Yükleniyor, boş liste ve hata durumlarının üçü de ekranda görünüyor
- [ ] `alert()` / `confirm()` yok (silme onayı için `<dialog>` kullan)
- [ ] Yeni sayfa `layout.js`'teki navigasyona eklendi (gerekiyorsa)
- [ ] Mobil genişlikte (≈375px) yatay kaydırma çıkmıyor
- [ ] Sadece `style.css`'e stil eklendi, CSS değişkenleri kullanıldı; inline `style=""` yok

## 4. Dene
```bash
docker compose up --build -d web
```
http://localhost:8080/<sayfa>.html — tarayıcı konsolunda hata olmamalı. Giriş yapmadan, müşteri olarak ve admin olarak ayrı ayrı aç.
