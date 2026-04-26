FROM dotnet-aspnet-local:8.0 AS base

RUN apt-get update && \
    apt-get install -y tzdata && \
    ln -snf /usr/share/zoneinfo/Asia/Tehran /etc/localtime && \
    echo "Asia/Tehran" > /etc/timezone && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM dotnet-sdk-local:8.0 AS build
WORKDIR /src

ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false

COPY nuget.config .
COPY ["sport-app-backend.csproj", "./"]
COPY ["projectTest/projectTest.csproj", "projectTest/"]

RUN dotnet restore "sport-app-backend.csproj"  --verbosity diagnostic --disable-parallel

COPY . .

RUN dotnet build "sport-app-backend.csproj" -c Release --no-restore

FROM build AS publish
RUN dotnet publish "sport-app-backend.csproj" -c Release -o /app/publish --no-build

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "sport-app-backend.dll"]