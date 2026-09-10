FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Timlog.Api/Timlog.Api.csproj", "Timlog.Api/"]
COPY ["Timlog.Application/Timlog.Application.csproj", "Timlog.Application/"]
COPY ["Timlog.Infrastructure/Timlog.Infrastructure.csproj", "Timlog.Infrastructure/"]
COPY ["Timlog.Domain/Timlog.Domain.csproj", "Timlog.Domain/"]
RUN dotnet restore "Timlog.Api/Timlog.Api.csproj"
COPY . .
WORKDIR "/src/Timlog.Api"
RUN dotnet publish "Timlog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5076
ENV ASPNETCORE_URLS=http://0.0.0.0:5076

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Timlog.Api.dll"]
