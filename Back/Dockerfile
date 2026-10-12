# API de Nudo (.NET 8). Contexto de construcción: la carpeta Back/.
#   docker build -t nudo-api Back
#   docker run -p 8080:8080 -e ConnectionStrings__Default="..." -e Jwt__Key="..." nudo-api

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Primero solo el proyecto: la restauración de paquetes queda en caché mientras no cambien las dependencias.
COPY Back.csproj ./
RUN dotnet restore Back.csproj
COPY . ./
RUN dotnet publish Back.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
# Corre como root a propósito: el disco persistente de Render (claves de Data Protection) se monta con dueño
# root y el usuario "app" de la imagen no podría escribir en él. Sin disco, se puede añadir "USER app".
ENTRYPOINT ["dotnet", "Back.dll"]
