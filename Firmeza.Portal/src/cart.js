export const TAX_RATE = 0.19

export function calculateTotals(lines) {
  const subtotal = roundMoney(lines.reduce((total, line) => total + Number(line.price) * Number(line.quantity), 0))
  const tax = roundMoney(subtotal * TAX_RATE)
  return { subtotal, tax, total: roundMoney(subtotal + tax) }
}

function roundMoney(amount) {
  return Math.sign(amount) * Math.round((Math.abs(amount) + Number.EPSILON) * 100) / 100
}

export function formatMoney(amount) {
  return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount)
}
