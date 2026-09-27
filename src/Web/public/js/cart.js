const CART_KEY = 'cart';

export function getCart() {
  const raw = localStorage.getItem(CART_KEY);
  if (!raw) return [];
  try {
    return JSON.parse(raw);
  } catch {
    return [];
  }
}

function saveCart(cart) {
  localStorage.setItem(CART_KEY, JSON.stringify(cart));
  window.dispatchEvent(new Event('cart-changed'));
}

export function addToCart(product, qty) {
  const cart = getCart();
  const existing = cart.find((item) => item.productId === product.id);
  if (existing) {
    existing.quantity += qty;
  } else {
    cart.push({ productId: product.id, name: product.name, price: product.price, quantity: qty });
  }
  saveCart(cart);
}

export function updateQuantity(productId, qty) {
  if (qty <= 0) {
    removeFromCart(productId);
    return;
  }
  const cart = getCart();
  const item = cart.find((i) => i.productId === productId);
  if (!item) return;
  item.quantity = qty;
  saveCart(cart);
}

export function removeFromCart(productId) {
  saveCart(getCart().filter((item) => item.productId !== productId));
}

export function clearCart() {
  saveCart([]);
}

export function cartCount() {
  return getCart().reduce((sum, item) => sum + item.quantity, 0);
}
