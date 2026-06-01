FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/BiliTool.Vn.Domain/BiliTool.Vn.Domain.csproj", "src/BiliTool.Vn.Domain/"]
COPY ["src/BiliTool.Vn.Application/BiliTool.Vn.Application.csproj", "src/BiliTool.Vn.Application/"]
COPY ["src/BiliTool.Vn.Infrastructure/BiliTool.Vn.Infrastructure.csproj", "src/BiliTool.Vn.Infrastructure/"]
COPY ["src/BiliTool.Vn.Web/BiliTool.Vn.Web.csproj", "src/BiliTool.Vn.Web/"]

# Restore
RUN dotnet restore "src/BiliTool.Vn.Web/BiliTool.Vn.Web.csproj"

# Copy all sources
COPY . .

# Build
WORKDIR "/src/src/BiliTool.Vn.Web"
RUN dotnet build "BiliTool.Vn.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiliTool.Vn.Web.csproj" -c Release -o /app/publish \
    --no-restore

FROM base AS final
WORKDIR /app
RUN mkdir -p logs
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "BiliTool.Vn.Web.dll"]
