FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/CleanArchitecture.Domain/CleanArchitecture.Domain.csproj src/CleanArchitecture.Domain/
COPY src/CleanArchitecture.Application/CleanArchitecture.Application.csproj src/CleanArchitecture.Application/
COPY src/CleanArchitecture.Infrastructure/CleanArchitecture.Infrastructure.csproj src/CleanArchitecture.Infrastructure/
COPY src/CleanArchitecture.WebApi/CleanArchitecture.WebApi.csproj src/CleanArchitecture.WebApi/
RUN dotnet restore src/CleanArchitecture.WebApi/CleanArchitecture.WebApi.csproj

COPY . .
RUN dotnet publish src/CleanArchitecture.WebApi/CleanArchitecture.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000

RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CleanArchitecture.WebApi.dll"]
