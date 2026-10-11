# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Fimple.Atm.Domain/Fimple.Atm.Domain.csproj Fimple.Atm.Domain/
COPY Fimple.Atm.Infrastructure/Fimple.Atm.Infrastructure.csproj Fimple.Atm.Infrastructure/
COPY Fimple.Atm.Application/Fimple.Atm.Application.csproj Fimple.Atm.Application/
COPY Fimple.Atm.Api/Fimple.Atm.Api.csproj Fimple.Atm.Api/
COPY Fimple.Atm.Worker/Fimple.Atm.Worker.csproj Fimple.Atm.Worker/
RUN dotnet restore Fimple.Atm.Api/Fimple.Atm.Api.csproj \
    && dotnet restore Fimple.Atm.Worker/Fimple.Atm.Worker.csproj

COPY Fimple.Atm.Domain/ Fimple.Atm.Domain/
COPY Fimple.Atm.Infrastructure/ Fimple.Atm.Infrastructure/
COPY Fimple.Atm.Application/ Fimple.Atm.Application/
COPY Fimple.Atm.Api/ Fimple.Atm.Api/
COPY Fimple.Atm.Worker/ Fimple.Atm.Worker/
RUN dotnet publish Fimple.Atm.Api/Fimple.Atm.Api.csproj -c Release -o /app/api --no-restore \
    && dotnet publish Fimple.Atm.Worker/Fimple.Atm.Worker.csproj -c Release -o /app/worker --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/api ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Fimple.Atm.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS worker
WORKDIR /app
COPY --from=build /app/worker ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Fimple.Atm.Worker.dll"]
