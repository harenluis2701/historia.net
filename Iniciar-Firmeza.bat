@echo off
setlocal
cd /d "%~dp0"

where docker >nul 2>nul
if errorlevel 1 (
  echo Docker no esta instalado. Instala Docker Desktop y vuelve a ejecutar este archivo.
  pause
  exit /b 1
)

docker info >nul 2>nul
if errorlevel 1 (
  echo Docker Desktop no esta iniciado. Abre Docker Desktop y vuelve a ejecutar este archivo.
  pause
  exit /b 1
)

if not exist .env copy .env.example .env >nul

echo Construyendo e iniciando Firmeza. La primera vez puede tardar varios minutos...
docker compose up --build -d
if errorlevel 1 (
  echo No se pudo iniciar Firmeza. Revisa el error anterior.
  pause
  exit /b 1
)

echo Firmeza esta lista:
echo Portal clientes: http://localhost:5173
echo Aplicacion web:  http://localhost:8080
echo API Swagger:     http://localhost:8081/swagger
start "" http://localhost:5173
pause
