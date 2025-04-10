FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["sport-app-backend.csproj", "./"]
RUN dotnet restore "./sport-app-backend.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "sport-app-backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sport-app-backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ConnectionString__DefaultConnection="server=char4set,3306;database=zen_mirzakhani;User=root;password=eYLc1zUCXeCZXcC8pdJEC0vf"
ENTRYPOINT ["dotnet", "sport-app-backend.dll"]