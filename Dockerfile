# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["CBAS.Web.csproj", "./"]
RUN dotnet restore "CBAS.Web.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "CBAS.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CBAS.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install PostgreSQL client + Tesseract OCR with English language data
RUN apt-get update && apt-get install -y \
    postgresql-client \
    tesseract-ocr \
    tesseract-ocr-eng \
    libtesseract-dev \
    libleptonica-dev \
    && rm -rf /var/lib/apt/lists/* \
    && echo "=== Tesseract version ===" && tesseract --version \
    && echo "=== tessdata location ===" && find /usr/share -name "eng.traineddata" 2>/dev/null \
    && echo "=== native libs ===" && ldconfig -p | grep -E "tesseract|lept"

# Set TESSDATA_PREFIX to the directory containing eng.traineddata
# Debian Bookworm: /usr/share/tesseract-ocr/5/tessdata
ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/5/tessdata

COPY --from=publish /app/publish .

# Create a non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "CBAS.Web.dll"]
