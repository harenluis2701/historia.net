# API ASP.NET Core .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Firmeza.sln ./
COPY Firmeza.Web/Firmeza.Web.csproj Firmeza.Web/
COPY Firmeza.Api/Firmeza.Api.csproj Firmeza.Api/
RUN dotnet restore Firmeza.Api/Firmeza.Api.csproj
COPY Firmeza.Web Firmeza.Web/
COPY Firmeza.Api Firmeza.Api/
RUN dotnet publish Firmeza.Api/Firmeza.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Firmeza.Api.dll"]
