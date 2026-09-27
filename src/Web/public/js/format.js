const currencyFormatter = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' });
const dateFormatter = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short' });

export function formatPrice(amount) {
  return currencyFormatter.format(amount);
}

export function formatDate(value) {
  return dateFormatter.format(new Date(value));
}
