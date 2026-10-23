import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { BrowserRouter, Link, Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { api } from './api'
import { calculateTotals, formatMoney } from './cart'
import './App.css'

const AuthContext = createContext(null)

function AuthProvider({ children }) {
  const [session, setSession] = useState(() => api.readSession())
  const navigate = useNavigate()

  useEffect(() => {
    const expired = () => {
      api.clearSession()
      setSession(null)
      navigate('/login', { replace: true, state: { message: 'Tu sesión terminó. Vuelve a iniciar sesión.' } })
    }
    window.addEventListener('firmeza:auth-expired', expired)
    return () => window.removeEventListener('firmeza:auth-expired', expired)
  }, [navigate])

  useEffect(() => {
    if (!session?.expiresAt) return undefined
    const remaining = session.expiresAt - Date.now()
    if (remaining <= 0) {
      window.dispatchEvent(new Event('firmeza:auth-expired'))
      return undefined
    }
    const timer = window.setTimeout(() => window.dispatchEvent(new Event('firmeza:auth-expired')), Math.min(remaining, 2_147_000_000))
    return () => window.clearTimeout(timer)
  }, [session])

  const value = useMemo(() => ({
    session,
    signIn: (auth) => {
      if (!auth.roles?.includes('Cliente')) throw new Error('Este portal es exclusivo para cuentas de cliente.')
      const saved = api.saveSession(auth)
      setSession(saved)
      return saved
    },
    signOut: () => {
      api.clearSession()
      setSession(null)
    },
  }), [session])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

function useAuth() {
  return useContext(AuthContext)
}

function ProtectedRoute({ children }) {
  const { session } = useAuth()
  const location = useLocation()
  if (!session) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  if (!session.roles?.includes('Cliente')) return <Navigate to="/login" replace />
  return children
}

function AppLayout({ children, customer }) {
  const { session, signOut } = useAuth()
  const location = useLocation()
  return (
    <div className="portal-shell">
      <header className="store-header">
        <Link to="/" className="store-brand" aria-label="Firmeza, catálogo">
          <span className="store-logo">F<span>.</span></span>
          <span><strong>FIRMEZA</strong><small>HERRAMIENTAS PARA CONSTRUIR</small></span>
        </Link>
        <nav className="store-nav" aria-label="Navegación principal">
          {session && <Link className={location.pathname === '/' ? 'selected' : ''} to="/">Productos</Link>}
          {session && <Link className={location.pathname === '/pedidos' ? 'selected' : ''} to="/pedidos">Mis compras</Link>}
        </nav>
        <div className="store-actions">
          {session ? <>
            <span className="customer-greeting"><span className="online-dot" />Hola, {customer?.fullName?.split(' ')[0] || session.email.split('@')[0]}</span>
            <button className="quiet-button" type="button" onClick={signOut}>Salir <span aria-hidden="true">↗</span></button>
          </> : <Link to="/login" className="button button-blue">Ingresar</Link>}
        </div>
      </header>
      {children}
      <footer className="store-footer"><span>© {new Date().getFullYear()} Firmeza</span><span>Materiales que construyen confianza.</span></footer>
    </div>
  )
}

function LoginPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [form, setForm] = useState({ email: '', password: '' })
  const [error, setError] = useState(location.state?.message || '')
  const [busy, setBusy] = useState(false)

  async function submit(event) {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      const auth = await api.login(form)
      signIn(auth)
      navigate(location.state?.from || '/', { replace: true })
    } catch (err) {
      setError(err.message)
    } finally { setBusy(false) }
  }

  return <AuthPage eyebrow="Bienvenido de nuevo" title="Entra a tu cuenta" intro="Encuentra lo que necesitas para tu próximo proyecto.">
    <form onSubmit={submit} className="auth-form">
      {error && <div className="notice notice-error" role="alert">{error}</div>}
      <label>Correo electrónico<input type="email" autoComplete="username" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} placeholder="tu@correo.com" /></label>
      <label>Contraseña<input type="password" autoComplete="current-password" required value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} placeholder="Tu contraseña" /></label>
      <button className="button button-blue button-wide" disabled={busy}>{busy ? 'Ingresando…' : 'Iniciar sesión'} <span aria-hidden="true">→</span></button>
      <p className="auth-switch">¿Aún no tienes cuenta? <Link to="/registro">Regístrate gratis</Link></p>
    </form>
  </AuthPage>
}

function RegisterPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ fullName: '', document: '', email: '', phone: '', password: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function submit(event) {
    event.preventDefault()
    setBusy(true)
    setError('')
    try {
      const auth = await api.register(form)
      signIn(auth)
      navigate('/', { replace: true })
    } catch (err) {
      setError(err.message)
    } finally { setBusy(false) }
  }

  return <AuthPage eyebrow="Crea tu cuenta" title="Compra fácil, construye mejor" intro="Regístrate para ver productos, hacer tus compras y consultar tus comprobantes.">
    <form onSubmit={submit} className="auth-form">
      {error && <div className="notice notice-error" role="alert">{error}</div>}
      <label>Nombre completo<input autoComplete="name" required maxLength="160" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} placeholder="Nombre y apellidos" /></label>
      <div className="form-pair">
        <label>Documento<input required maxLength="30" value={form.document} onChange={(e) => setForm({ ...form, document: e.target.value })} placeholder="Número de documento" /></label>
        <label>Teléfono<input autoComplete="tel" maxLength="40" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} placeholder="300 000 0000" /></label>
      </div>
      <label>Correo electrónico<input type="email" autoComplete="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} placeholder="tu@correo.com" /></label>
      <label>Contraseña<input type="password" autoComplete="new-password" minLength="10" required value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} placeholder="Mínimo 10 caracteres" /><small>Incluye mayúsculas, minúsculas, un número y un símbolo.</small></label>
      <button className="button button-blue button-wide" disabled={busy}>{busy ? 'Creando cuenta…' : 'Crear cuenta'} <span aria-hidden="true">→</span></button>
      <p className="auth-switch">¿Ya tienes cuenta? <Link to="/login">Inicia sesión</Link></p>
    </form>
  </AuthPage>
}

function AuthPage({ eyebrow, title, intro, children }) {
  return <AppLayout><main className="auth-layout">
    <section className="auth-aside">
      <span className="hero-kicker">FERRETERÍA FIRMEZA</span>
      <h1>Tu próximo<br /><em>proyecto</em><br />empieza aquí.</h1>
      <p>Materiales y herramientas para hacer realidad tus ideas.</p>
      <div className="auth-spark" aria-hidden="true">⚒</div>
      <div className="auth-perks"><span>✓ Compra segura</span><span>✓ Recibos digitales</span></div>
    </section>
    <section className="auth-card-wrap"><div className="auth-card">
      <div className="eyebrow">{eyebrow}</div><h2>{title}</h2><p className="auth-intro">{intro}</p>
      {children}
    </div></section>
  </main></AppLayout>
}

function CatalogPage() {
  const { session } = useAuth()
  const [customer, setCustomer] = useState(null)
  const [products, setProducts] = useState([])
  const [cart, setCart] = useState({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState(null)
  const [search, setSearch] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let active = true
    Promise.all([api.getCustomer(), api.getProducts()])
      .then(([profile, items]) => { if (active) { setCustomer(profile); setProducts(items) } })
      .catch((err) => { if (active) setError(err.message) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [session?.token])

  const filteredProducts = products.filter((product) => `${product.name} ${product.description}`.toLocaleLowerCase().includes(search.toLocaleLowerCase()))
  const cartLines = Object.values(cart).map(({ product, quantity }) => ({ ...product, quantity }))
  const totals = calculateTotals(cartLines)

  function add(product) {
    setCart((current) => {
      const existing = current[product.id]?.quantity || 0
      if (existing >= product.stock) return current
      return { ...current, [product.id]: { product, quantity: existing + 1 } }
    })
    setNotice(null)
  }

  function changeQuantity(id, delta) {
    setCart((current) => {
      const line = current[id]
      if (!line) return current
      const quantity = Math.min(line.product.stock, line.quantity + delta)
      if (quantity <= 0) { const { [id]: removed, ...rest } = current; return rest }
      return { ...current, [id]: { ...line, quantity } }
    })
  }

  async function checkout() {
    if (!customer || cartLines.length === 0) return
    setBusy(true)
    setError('')
    try {
      const result = await api.createSale({ customerId: customer.id, items: cartLines.map((line) => ({ productId: line.id, quantity: line.quantity })) })
      setCart({})
      setNotice({ sale: result.sale, emailSent: result.emailSent })
      setProducts(await api.getProducts())
    } catch (err) {
      setError(err.message)
    } finally { setBusy(false) }
  }

  return <AppLayout customer={customer}>
    <main className="catalog-main">
      <section className="shop-hero">
        <div className="hero-grid" aria-hidden="true" />
        <div className="shop-hero-copy"><span className="hero-kicker">CALIDAD PARA CADA OBRA</span><h1>Construye en grande.<br /><em>Nosotros te ayudamos.</em></h1><p>Herramientas y materiales confiables para darle forma a tus proyectos.</p><a href="#catalogo" className="button button-yellow">Explorar catálogo <span aria-hidden="true">↓</span></a></div>
        <div className="shop-hero-art" aria-hidden="true"><div className="sun-disc" /><div className="tool-emblem">⚒</div><span className="hero-slogan">HECHO PARA<br />DURAR</span></div>
      </section>

      {notice && <section className={`purchase-confirmation ${notice.emailSent ? 'sent' : 'pending'}`} role="status">
        <div className="confirm-mark">{notice.emailSent ? '✓' : '!'}</div>
        <div><strong>¡Compra registrada! Pedido #{notice.sale.id}</strong><p>{notice.emailSent ? 'Enviamos el comprobante PDF a tu correo.' : 'No pudimos enviar el correo. Tu compra quedó registrada y puedes descargar el comprobante.'}</p></div>
        <button className="button button-dark" type="button" onClick={() => api.downloadReceipt(notice.sale.id)}>Descargar PDF <span aria-hidden="true">↓</span></button>
        <button className="icon-close" type="button" aria-label="Cerrar aviso" onClick={() => setNotice(null)}>×</button>
      </section>}
      {error && <div className="notice notice-error page-notice" role="alert">{error}<button type="button" onClick={() => setError('')} aria-label="Cerrar">×</button></div>}

      <div className="catalog-toolbar" id="catalogo">
        <div><div className="eyebrow">LO QUE NECESITAS</div><h2>Productos disponibles</h2><p>Elige lo que te hace falta para seguir construyendo.</p></div>
        <label className="search-box"><span aria-hidden="true">⌕</span><input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Buscar herramienta o material" aria-label="Buscar productos" /></label>
      </div>

      {loading ? <div className="loading-state"><span className="spinner" />Cargando catálogo…</div> : filteredProducts.length ? <div className="shop-content">
        <section className="product-grid" aria-label="Catálogo de productos">{filteredProducts.map((product, index) => <ProductCard key={product.id} product={product} index={index} onAdd={add} />)}</section>
        <CartPanel lines={cartLines} totals={totals} changeQuantity={changeQuantity} checkout={checkout} busy={busy} customer={customer} />
      </div> : <div className="empty-catalog"><div className="empty-icon">⌕</div><h3>{products.length ? 'No encontramos productos' : 'Estamos preparando el inventario'}</h3><p>{products.length ? 'Prueba con otra palabra para encontrar lo que buscas.' : 'En este momento no hay productos con existencias disponibles. Vuelve pronto o contáctanos para consultar disponibilidad.'}</p>{search && <button className="button button-outline" onClick={() => setSearch('')}>Limpiar búsqueda</button>}</div>}
    </main>
  </AppLayout>
}

function ProductCard({ product, index, onAdd }) {
  const symbols = ['⚒', '▦', '⌁', '🔩']
  return <article className="product-card">
    <div className={`product-art art-${index % 4}`}><span className="product-tag">DISPONIBLE</span><span className="product-symbol">{symbols[index % symbols.length]}</span><span className="art-number">F / {String(product.id).padStart(2, '0')}</span></div>
    <div className="product-info"><span className="product-category">FERRETERÍA</span><h3>{product.name}</h3><p>{product.description || 'Calidad confiable para tus proyectos de construcción.'}</p><div className="product-bottom"><div><strong>{formatMoney(product.price)}</strong><small>{product.stock} disponibles</small></div><button className="add-button" onClick={() => onAdd(product)} aria-label={`Agregar ${product.name} al carrito`}>＋</button></div></div>
  </article>
}

function CartPanel({ lines, totals, changeQuantity, checkout, busy, customer }) {
  return <aside className="cart-panel" aria-label="Carrito de compras">
    <div className="cart-heading"><div><div className="eyebrow">TU PEDIDO</div><h2>Carrito <span>{lines.reduce((sum, line) => sum + line.quantity, 0)}</span></h2></div><span className="cart-basket">▱</span></div>
    {!lines.length ? <div className="cart-empty"><div>▱</div><strong>Tu carrito está vacío</strong><p>Agrega productos para ver aquí el resumen de tu compra.</p></div> : <>
      <div className="cart-lines">{lines.map((line) => <div className="cart-line" key={line.id}>
        <div className="cart-line-icon">⚒</div><div className="cart-line-main"><strong>{line.name}</strong><small>{formatMoney(line.price)} c/u</small><div className="quantity-control"><button type="button" onClick={() => changeQuantity(line.id, -1)} aria-label={`Quitar una unidad de ${line.name}`}>−</button><span>{line.quantity}</span><button type="button" onClick={() => changeQuantity(line.id, 1)} aria-label={`Agregar una unidad de ${line.name}`}>＋</button></div></div><strong className="line-total">{formatMoney(line.price * line.quantity)}</strong>
      </div>)}</div>
      <div className="cart-totals"><div><span>Subtotal</span><strong>{formatMoney(totals.subtotal)}</strong></div><div><span>IVA (19%)</span><strong>{formatMoney(totals.tax)}</strong></div><div className="grand-total"><span>Total</span><strong>{formatMoney(totals.total)}</strong></div></div>
      <button className="button button-blue button-wide checkout-button" onClick={checkout} disabled={busy || !customer}>{busy ? 'Procesando compra…' : 'Confirmar compra'} <span aria-hidden="true">→</span></button>
      <div className="secure-note"><span>♢</span> Compra protegida · Inventario verificado</div>
    </>}
  </aside>
}

function OrdersPage() {
  const { session } = useAuth()
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  useEffect(() => {
    api.getSales().then(setOrders).catch((err) => setError(err.message)).finally(() => setLoading(false))
  }, [session?.token])
  return <AppLayout><main className="orders-main">
    <div className="page-title"><div><div className="eyebrow">TU CUENTA</div><h1>Mis compras</h1><p>Consulta tus pedidos y descarga los comprobantes.</p></div><Link className="button button-outline" to="/">← Volver al catálogo</Link></div>
    {error && <div className="notice notice-error" role="alert">{error}</div>}
    {loading ? <div className="loading-state"><span className="spinner" />Cargando compras…</div> : orders.length ? <div className="orders-list">{orders.map((order) => <article className="order-card" key={order.id}>
      <div className="order-ident"><span className="order-icon">✓</span><div><strong>Pedido #{order.id}</strong><small>{new Date(order.createdAt).toLocaleString('es-CO')}</small></div></div>
      <div className="order-products">{order.items.map((item) => <span key={item.productId}>{item.quantity} × {item.productName}</span>)}</div>
      <strong className="order-total">{formatMoney(order.total)}</strong>
      <button className="button button-outline" onClick={() => api.downloadReceipt(order.id)}>Comprobante PDF ↓</button>
    </article>)}</div> : <div className="empty-catalog compact-empty"><div className="empty-icon">▤</div><h3>Aún no tienes compras</h3><p>Cuando realices un pedido, aquí podrás consultar el detalle y descargar el comprobante.</p><Link className="button button-blue" to="/">Explorar productos</Link></div>}
  </main></AppLayout>
}

function PortalRoutes() {
  return <Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route path="/registro" element={<RegisterPage />} />
    <Route path="/" element={<ProtectedRoute><CatalogPage /></ProtectedRoute>} />
    <Route path="/pedidos" element={<ProtectedRoute><OrdersPage /></ProtectedRoute>} />
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
}

function App() {
  return <BrowserRouter><AuthProvider><PortalRoutes /></AuthProvider></BrowserRouter>
}

export default App
