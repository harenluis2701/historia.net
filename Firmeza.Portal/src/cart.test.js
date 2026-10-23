import test from 'node:test'
import assert from 'node:assert/strict'
import { calculateTotals } from './cart.js'

test('calcula subtotal, IVA del 19% y total para el carrito', () => {
  assert.deepEqual(calculateTotals([
    { price: 12900.5, quantity: 3 },
    { price: 5000, quantity: 2 },
  ]), { subtotal: 48701.5, tax: 9253.29, total: 57954.79 })
})

test('devuelve totales en cero para un carrito vacío', () => {
  assert.deepEqual(calculateTotals([]), { subtotal: 0, tax: 0, total: 0 })
})
