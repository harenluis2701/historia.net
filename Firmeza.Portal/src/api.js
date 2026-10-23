const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:8081/api').replace(/\/$/, '')
const SESSION_KEY = 'firmeza.customer.session'

export class ApiError extends Error {
  constructor(message, status) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

function clearSession() {
  window.sessionStorage.removeItem(SESSION_KEY)
}

function expiredSession() {
  clearSession()
  window.dispatchEvent(new Event('firmeza:auth-expired'))
  return new ApiError('Tu sesión terminó. Vuelve a iniciar sesión.', 401)
}

function readSession() {
  try {
    const raw = window.sessionStorage.getItem(SESSION_KEY)
    if (!raw) return null
    const saved = JSON.parse(raw)
    if (!saved.token || !saved.roles?.includes('Cliente')) {
      clearSession()
      return null
    }
    if (saved.expiresAt <= Date.now()) {
      clearSession()
      return null
    }
    return saved
  } catch {
    clearSession()
    return null
  }
}

function saveSession(auth) {
  const saved = {
    token: auth.accessToken,
    email: auth.email,
    roles: auth.roles || [],
    expiresAt: new Date(auth.expiresAtUtc).getTime(),
  }
  if (!Number.isFinite(saved.expiresAt) || saved.expiresAt <= Date.now()) throw new ApiError('La API devolvió una sesión vencida.', 401)
  window.sessionStorage.setItem(SESSION_KEY, JSON.stringify(saved))
  return saved
}

async function request(path, { method = 'GET', body, auth = true } = {}) {
  const session = readSession()
  if (auth && !session) throw expiredSession()
  const headers = { Accept: 'application/json' }
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (auth) headers.Authorization = `Bearer ${session.token}`

  let response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) })
  } catch {
    throw new ApiError('No pudimos conectar con Firmeza. Comprueba que la API esté disponible.', 0)
  }

  if (response.status === 401 && auth) throw expiredSession()
  const text = await response.text()
  let data = null
  if (text) {
    try { data = JSON.parse(text) } catch { data = text }
  }
  if (!response.ok) {
    const validation = data?.errors && Object.values(data.errors).flat().join(' ')
    throw new ApiError(validation || data?.message || data?.title || 'No se pudo completar la solicitud.', response.status)
  }
  return { data, headers: response.headers }
}

async function downloadReceipt(saleId) {
  const session = readSession()
  if (!session) throw expiredSession()
  let response
  try {
    response = await fetch(`${API_BASE_URL}/sales/${saleId}/receipt`, { headers: { Authorization: `Bearer ${session.token}` } })
  } catch {
    throw new ApiError('No pudimos conectar con Firmeza para descargar el comprobante.', 0)
  }
  if (response.status === 401) throw expiredSession()
  if (!response.ok) throw new ApiError('No pudimos descargar el comprobante.', response.status)
  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `comprobante-firmeza-${saleId}.pdf`
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
}

export const api = {
  readSession,
  saveSession,
  clearSession,
  login: async (credentials) => (await request('/auth/login', { method: 'POST', body: credentials, auth: false })).data,
  register: async (details) => (await request('/auth/register', { method: 'POST', body: details, auth: false })).data,
  getCustomer: async () => (await request('/customers/me')).data,
  getProducts: async () => (await request('/products?inStock=true')).data,
  getSales: async () => (await request('/sales')).data,
  createSale: async (sale) => {
    const { data, headers } = await request('/sales', { method: 'POST', body: sale })
    return { sale: data, emailSent: headers.get('X-Email-Sent') === 'true' }
  },
  downloadReceipt,
}
