FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/ExamShield.Domain/ExamShield.Domain.csproj",          "src/ExamShield.Domain/"]
COPY ["src/ExamShield.Application/ExamShield.Application.csproj", "src/ExamShield.Application/"]
COPY ["src/ExamShield.Infrastructure/ExamShield.Infrastructure.csproj", "src/ExamShield.Infrastructure/"]
COPY ["src/ExamShield.Api/ExamShield.Api.csproj",                 "src/ExamShield.Api/"]

RUN dotnet restore "src/ExamShield.Api/ExamShield.Api.csproj"

COPY . .
RUN dotnet build "src/ExamShield.Api/ExamShield.Api.csproj" \
    -c $BUILD_CONFIGURATION --no-restore -o /app/build

FROM build AS publish
RUN dotnet publish "src/ExamShield.Api/ExamShield.Api.csproj" \
    -c $BUILD_CONFIGURATION --no-build -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Non-root user for least-privilege runtime
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "ExamShield.Api.dll"]
