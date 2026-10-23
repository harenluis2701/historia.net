#!/usr/bin/env sh
set -eu

PROJECT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$PROJECT_DIR"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker no está instalado. Instala Docker Engine y el complemento Docker Compose."
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "El servicio Docker no está iniciado o tu usuario no tiene permiso para usarlo."
  echo "Inicia Docker y vuelve a ejecutar este script."
  exit 1
fi

if [ ! -f .env ]; then
  cp .env.example .env
  echo "Se creó .env desde .env.example. Puedes configurar SMTP y otras opciones allí."
fi

echo "Construyendo e iniciando Firmeza. La primera vez puede tardar varios minutos..."
docker compose up --build -d

echo
echo "Firmeza está lista:"
echo "  Portal clientes: http://localhost:5173"
echo "  Aplicación web:  http://localhost:8080"
echo "  API Swagger:     http://localhost:8081/swagger"

if command -v xdg-open >/dev/null 2>&1; then
  xdg-open http://localhost:5173 >/dev/null 2>&1 || true
elif command -v open >/dev/null 2>&1; then
  open http://localhost:5173 >/dev/null 2>&1 || true
fi
