FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ExamShield.Api/ExamShield.Api.csproj", "src/ExamShield.Api/"]
COPY ["src/ExamShield.Application/ExamShield.Application.csproj", "src/ExamShield.Application/"]
COPY ["src/ExamShield.Domain/ExamShield.Domain.csproj", "src/ExamShield.Domain/"]
COPY ["src/ExamShield.Infrastructure/ExamShield.Infrastructure.csproj", "src/ExamShield.Infrastructure/"]
RUN dotnet restore "src/ExamShield.Api/ExamShield.Api.csproj"
COPY . .
WORKDIR "/src/src/ExamShield.Api"
RUN dotnet build "ExamShield.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ExamShield.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExamShield.Api.dll"]
