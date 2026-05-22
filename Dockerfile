FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS build
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AfriPay.csproj", "./"]
RUN dotnet restore "AfriPay.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "AfriPay.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AfriPay.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AfriPay.dll"]
