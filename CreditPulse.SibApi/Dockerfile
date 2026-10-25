# Se construye desde la RAÍZ del repositorio (el contexto debe incluir
# CreditPulse.SibSimulador, de donde se toman los archivos generadores):
#   docker build -f CreditPulse.SibApi/Dockerfile -t cp-sib-api .
# (docker-compose.yml ya lo hace con "docker compose up -d --build")
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CreditPulse.SibApi/ CreditPulse.SibApi/
COPY CreditPulse.SibSimulador/Models/ CreditPulse.SibSimulador/Models/
RUN dotnet publish CreditPulse.SibApi/CreditPulse.SibApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CreditPulse.SibApi.dll"]
