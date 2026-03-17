# Use the official .NET 10 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy the solution file and project files first to cache NuGet restores
COPY ["AndysDataGateway.slnx", "./"]
COPY ["src/Gateway.API/Gateway.API.csproj", "src/Gateway.API/"]
COPY ["src/Gateway.Core/Gateway.Core.csproj", "src/Gateway.Core/"]
COPY ["src/Gateway.Infrastructure/Gateway.Infrastructure.csproj", "src/Gateway.Infrastructure/"]
COPY ["tests/Gateway.Tests/Gateway.Tests.csproj", "tests/Gateway.Tests/"]

RUN dotnet restore "AndysDataGateway.slnx"

# Copy the rest of the source code and build
COPY . .
WORKDIR "/app/src/Gateway.API"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Use the lightweight ASP.NET runtime for the final image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose the standard web ports
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gateway.API.dll"]