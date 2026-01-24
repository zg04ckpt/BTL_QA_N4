# build project
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY . . 
RUN dotnet restore 
RUN dotnet publish WebAPI/WebAPI.csproj -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# final image to run application
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
RUN apk add --no-cache tzdata
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebAPI.dll"] 