# 1. Gamitin ang .NET 8 SDK para ma-build ang code
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopyahin ang .csproj at i-restore ang dependencies
COPY ["WeShare.API.csproj", "./"]
RUN dotnet restore "./WeShare.API.csproj"

# Kopyahin ang lahat ng files at i-build
COPY . .
RUN dotnet publish "WeShare.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Gamitin ang ASP.NET runtime para i-run ang app sa Hugging Face
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# CRITICAL: Ito ang port na hinihingi ng Hugging Face!
EXPOSE 7860
ENV ASPNETCORE_URLS=http://+:7860

COPY --from=build /app/publish .

# Patakbuhin ang API
ENTRYPOINT ["dotnet", "WeShare.API.dll"]