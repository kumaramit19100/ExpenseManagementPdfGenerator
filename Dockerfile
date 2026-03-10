# -------- BASE RUNTIME (Playwright included) --------
FROM mcr.microsoft.com/playwright/dotnet:v1.58.0-jammy AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["ExpenseManagementPdfGenerator.csproj", "./"]
RUN dotnet restore "ExpenseManagementPdfGenerator.csproj"

COPY . .
RUN dotnet build "ExpenseManagementPdfGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/build

# -------- PUBLISH STAGE --------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ExpenseManagementPdfGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# -------- FINAL STAGE --------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ExpenseManagementPdfGenerator.dll"]