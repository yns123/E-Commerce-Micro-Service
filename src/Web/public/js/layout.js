import { isLoggedIn, isAdmin, logout } from './auth.js';
import { cartCount } from './cart.js';

export function renderHeader() {
  const header = document.getElementById('site-header');
  if (!header) return;

  render();
  window.addEventListener('cart-changed', render);

  function render() {
    header.replaceChildren();

    const nav = document.createElement('nav');
    nav.className = 'nav';

    const logo = document.createElement('a');
    logo.className = 'nav__logo';
    logo.href = '/index.html';
    logo.textContent = 'Mağaza';
    nav.append(logo);

    const links = document.createElement('div');
    links.className = 'nav__links';

    links.append(makeLink('/index.html', 'Ürünler'));
    links.append(makeLink('/cart.html', `Sepet (${cartCount()})`));

    if (isLoggedIn()) {
      links.append(makeLink('/orders.html', 'Siparişlerim'));
      if (isAdmin()) {
        links.append(makeLink('/admin.html', 'Admin'));
      }

      const logoutButton = document.createElement('button');
      logoutButton.type = 'button';
      logoutButton.className = 'nav__link nav__link--button';
      logoutButton.textContent = 'Çıkış';
      logoutButton.addEventListener('click', logout);
      links.append(logoutButton);
    } else {
      links.append(makeLink('/login.html', 'Giriş'));
      links.append(makeLink('/register.html', 'Kayıt Ol'));
    }

    nav.append(links);
    header.append(nav);
  }

  function makeLink(href, text) {
    const a = document.createElement('a');
    a.href = href;
    a.className = 'nav__link';
    a.textContent = text;
    return a;
  }
}
