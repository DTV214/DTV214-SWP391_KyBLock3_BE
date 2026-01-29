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

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 5000

# Switch to non-root user
USER appuser

# Start the application
ENTRYPOINT ["dotnet", "TetGift.dll"]
