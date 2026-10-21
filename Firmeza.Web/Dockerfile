# Etapa 1: Imagen base para compilar el código (SDK de .NET 8.0)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia primero los archivos de proyecto para restaurar dependencias
COPY *.csproj ./
RUN dotnet restore

# Copia todo el resto del código y lo compila
COPY . ./
RUN dotnet publish -c Release -o out

# Etapa 2: Imagen base ligera solo para ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Expone el puerto 8080 (el por defecto de .NET 8 en Docker)
EXPOSE 8080

# Comando para iniciar la aplicación
ENTRYPOINT ["dotnet", "Firmeza.Web.dll"]