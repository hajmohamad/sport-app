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
ENV ConnectionString__DefaultConnection="server=charset,3306;database=funny_carver;User=root;password=Cm7uXDuIyvwNc8d33OAuJlRs"
ENTRYPOINT ["dotnet", "sport-app-backend.dll"]