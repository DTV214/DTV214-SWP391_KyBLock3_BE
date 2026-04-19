# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TetGift.DAL/TetGift.DAL.csproj TetGift.DAL/
COPY TetGift.BLL/TetGift.BLL.csproj TetGift.BLL/
COPY TetGift/TetGift.csproj TetGift/

RUN dotnet restore TetGift/TetGift.csproj

COPY . .

WORKDIR /src/TetGift
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

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
    libglib2.0-0 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
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

COPY --from=build /app/publish ./

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

RUN mkdir -p /ms-playwright && \
    adduser --disabled-password --gecos "" appuser && \
    chown -R appuser:appuser /app /ms-playwright && \
    chmod -R 755 /app /ms-playwright

EXPOSE 5000

USER appuser

ENTRYPOINT ["dotnet", "TetGift.dll"]