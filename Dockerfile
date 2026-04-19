# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first for better layer caching
COPY TetGift.DAL/TetGift.DAL.csproj TetGift.DAL/
COPY TetGift.BLL/TetGift.BLL.csproj TetGift.BLL/
COPY TetGift/TetGift.csproj TetGift/

# Restore dependencies
RUN dotnet restore TetGift/TetGift.csproj

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/TetGift
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install system dependencies needed by Playwright/Chromium
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    wget \
    curl \
    gnupg \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libc6 \
    libcairo2 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libexpat1 \
    libfontconfig1 \
    libgbm1 \
    libgcc1 \
    libglib2.0-0 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
    libstdc++6 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libxshmfence1 \
    libxss1 \
    libxtst6 \
    fonts-liberation \
    fonts-dejavu-core \
    xdg-utils \
    && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish ./

# Install Playwright Chromium while still root
# publish output of Microsoft.Playwright includes playwright.sh
RUN chmod +x ./playwright.sh && \
    ./playwright.sh install chromium

# Fix permissions for Playwright files
RUN chmod -R 755 /app && \
    if [ -d /root/.cache/ms-playwright ]; then chmod -R 755 /root/.cache/ms-playwright; fi

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && \
    chown -R appuser:appuser /app

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PLAYWRIGHT_BROWSERS_PATH=/root/.cache/ms-playwright

# Expose port
EXPOSE 5000

# Switch to non-root user
USER appuser

# Start the application
ENTRYPOINT ["dotnet", "TetGift.dll"]