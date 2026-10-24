FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src
COPY . .

RUN dotnet restore Firmeza.sln
RUN dotnet build Firmeza.sln --configuration Release --no-restore

ENTRYPOINT ["dotnet", "test", "Firmeza.Tests/Firmeza.Tests.csproj", "--configuration", "Release", "--no-build", "--no-restore", "--logger", "console;verbosity=normal"]
