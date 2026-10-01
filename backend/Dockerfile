FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ src/

ARG PROJECT=ITFPulse.Api

RUN dotnet publish src/${PROJECT}/${PROJECT}.csproj \
    -c Release \
    -o /app


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet"]