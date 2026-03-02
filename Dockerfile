# 1. Usar la imagen oficial del SDK de .NET para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# 2. Copiar todo el código y restaurar dependencias
COPY . ./
RUN dotnet restore

# 3. Compilar la aplicación
RUN dotnet publish -c Release -o out

# 4. Usar la imagen oficial de .NET para ejecutar (más ligera)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .

# 5. Configurar el puerto que usa Render por defecto
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# 6. Ejecutar la app (Asegúrate de que este sea el nombre de tu proyecto)
ENTRYPOINT ["dotnet", "Obras.Api.dll"]