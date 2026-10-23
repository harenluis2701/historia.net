# Portal de clientes Firmeza

SPA en React y Vite para que clientes creen una cuenta, consulten productos disponibles, armen un carrito, compren y descarguen sus comprobantes.

## Requisitos

- Node.js 22.12 o posterior.
- API Firmeza en `http://localhost:8081` (por ejemplo, iniciada con Docker Compose).

## Instalar y ejecutar

```powershell
npm install
Copy-Item .env.example .env
npm run dev
```

Abre `http://localhost:5173`. La variable `VITE_API_BASE_URL` del archivo `.env` permite cambiar la dirección base; su valor predeterminado es `http://localhost:8081/api`.

La API debe permitir el origen `http://localhost:5173` mediante `Cors:AllowedOrigins`. Docker Compose ya configura ese origen por defecto. Si se ejecuta la API directamente con el perfil Development, se encuentra configurado en `appsettings.Development.json`.

## Verificación

```powershell
npm test
npm run build
```

## Seguridad y endpoints usados

El portal conserva el JWT en `sessionStorage`, añade `Authorization: Bearer` a las solicitudes y cierra la sesión cuando vence o la API responde `401`. Rechaza cuentas que no tengan rol `Cliente`. Solo consume registro/login, perfil propio, productos disponibles, compras e historial propio, y descarga de comprobantes. La API también verifica que un cliente solo descargue sus recibos.

## Comprobantes por correo

Después de registrar una compra, la API genera el PDF y lo adjunta al correo de confirmación. Para envío real configura `SMTP_USER`, `SMTP_PASSWORD` (contraseña de aplicación de Gmail) y `SMTP_FROM_ADDRESS` al ejecutar Docker Compose. Sin SMTP, la compra se confirma y el PDF se puede descargar, pero el portal indica que el correo no fue enviado.
