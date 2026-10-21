# Etapa de compilación con el SDK de .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Imagen de ejecución ligera
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
RUN mkdir -p /app/wwwroot/recibos && chown -R $APP_UID /app/wwwroot/recibos
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Firmeza.Web.dll"]
