# Frontend

## Kesin kurallar
- Sadece HTML, CSS, vanilla JavaScript. **Kütüphane yok**: React, Vue, jQuery, Bootstrap, Tailwind, Axios, CDN'den script — hiçbiri.
- npm, package.json, build/bundle adımı yok. Dosyalar olduğu gibi nginx'ten servis edilir.
- JS dosyaları ES module: `<script type="module" src="js/pages/products.js"></script>`
- API çağrıları sadece `js/api.js` üzerinden yapılır (sayfalar doğrudan `fetch` çağırmaz).
- Kullanıcıdan/API'den gelen metin DOM'a `textContent` ile yazılır. **`innerHTML` ile veri basma** (XSS). innerHTML sadece sabit, veri içermeyen şablonlar için kullanılabilir.
- API adresi göreli: `/api/...` (nginx aynı origin'den gateway'e yönlendirir).

## Dosya yapısı
```
src/Web/
  Dockerfile                # FROM nginx:alpine, COPY public → /usr/share/nginx/html, COPY nginx.conf
  nginx.conf                # "/" statik, "/api/" → http://gateway:8080
  public/
    index.html              # ürün listesi + arama
    product.html            # ?id=... ürün detayı, "Sepete ekle"
    cart.html               # sepet, miktar değiştir, "Siparişi ver"
    login.html
    register.html
    orders.html             # siparişlerim, Pending olanlar için durum takibi
    admin.html              # sadece Admin: ürün ekle/düzenle/sil
    css/style.css
    js/
      api.js                # fetch sarmalayıcı
      auth.js               # token saklama, giriş durumu, rol
      cart.js               # localStorage sepet
      layout.js             # ortak header/nav'ı her sayfaya basar
      format.js             # para ve tarih biçimlendirme (tr-TR, TRY)
      pages/
        index.js, product.js, cart.js, login.js, register.js, orders.js, admin.js
```

## Modüllerin sözleşmesi

### api.js
```js
export async function api(path, { method = 'GET', body } = {}) // → JSON veya null (204)
```
- `Authorization` header'ını token varsa otomatik ekler.
- Yanıt `ok` değilse `ApiError { status, message, fieldErrors }` fırlatır (message = ProblemDetails.title || detail).
- 401 gelirse token'ı siler ve `login.html?returnUrl=<şu anki sayfa>` adresine yönlendirir.

### auth.js
- Token `localStorage['token']`, kullanıcı bilgisi `localStorage['user']` (`{email, role, expiresAt}`).
- `isLoggedIn()`, `isAdmin()`, `getUser()`, `saveSession(loginResponse)`, `logout()`
- `requireLogin()` ve `requireAdmin()` sayfa başında çağrılır; koşul sağlanmazsa yönlendirir.
- Süresi dolmuş token girişsiz sayılır.

### cart.js
- `localStorage['cart']` = `[{productId, name, price, quantity}]` (fiyat sadece gösterim içindir; sunucu kendi fiyatını kullanır).
- `getCart()`, `addToCart(product, qty)`, `updateQuantity(productId, qty)`, `removeFromCart(productId)`, `clearCart()`, `cartCount()`
- Değişince `window.dispatchEvent(new Event('cart-changed'))` → header'daki sayaç güncellenir.

## Davranışlar
- Her sayfa: `layout.js` ile header (logo, Ürünler, Sepet (n), Siparişlerim, Admin [sadece admin], Giriş/Çıkış).
- Yükleniyor durumu gösterilir; hata olursa sayfada okunur bir mesaj kutusu çıkar (alert() kullanma).
- Sipariş verme: `POST /api/ordering/orders` → 202 → sepeti temizle → `orders.html?highlight=<orderId>`'e git.
- orders.html: `Pending` sipariş varsa 2 saniyede bir listeyi yeniden çek; hiç Pending kalmayınca veya 30 saniye geçince dur.
- Para: `new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' })`.
- Formlar: `<form>` + `submit` event + `event.preventDefault()`; HTML5 doğrulama özellikleri (`required`, `type="email"`, `min`) kullanılır.

## Görünüm
- Tek `style.css`, CSS değişkenleri (`:root { --color-primary: ... }`), flexbox/grid, mobilde de düzgün (ürün kartları `grid-template-columns: repeat(auto-fill, minmax(220px, 1fr))`).
- Sade ve temiz; animasyon/efekt gerekmez.
- Ürün resmi yoksa gri bir yer tutucu kutu gösterilir (harici resim servisi kullanma).
