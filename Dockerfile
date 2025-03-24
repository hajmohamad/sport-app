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

# مرحله جدید: بروزرسانی پایگاه داده
FROM base AS update_db
WORKDIR /app
COPY --from=publish /app/publish . 
ENV ConnectionString__DefaultConnection="Server=charset;Port=3306;Database=blissful_chaum;User Id=root;Password=F1UkJlJLGSPIylKYfI0slYeg;"

# اجرای دستور dotnet ef database update
RUN dotnet ef database update --no-build --environment Production

# مرحله نهایی: اجرای اپلیکیشن
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish . 
ENTRYPOINT ["dotnet", "sport-app-backend.dll"]
