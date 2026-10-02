FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ITFPulse.Api/ITFPulse.Api.csproj src/ITFPulse.Api/
COPY src/ITFPulse.Application/ITFPulse.Application.csproj src/ITFPulse.Application/
COPY src/ITFPulse.Contracts/ITFPulse.Contracts.csproj src/ITFPulse.Contracts/
COPY src/ITFPulse.Domain/ITFPulse.Domain.csproj src/ITFPulse.Domain/
COPY src/ITFPulse.Infrastructure/ITFPulse.Infrastructure.csproj src/ITFPulse.Infrastructure/
RUN dotnet restore src/ITFPulse.Api/ITFPulse.Api.csproj
COPY src/ src/
RUN dotnet publish src/ITFPulse.Api/ITFPulse.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ITFPulse.Api.dll"]
