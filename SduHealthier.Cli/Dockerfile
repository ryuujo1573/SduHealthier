FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["SduHealthier.Cli/SduHealthier.Cli.csproj", "SduHealthier.Cli/"]
RUN dotnet restore "SduHealthier.Cli/SduHealthier.Cli.csproj"
COPY . .
WORKDIR "/src/SduHealthier.Cli"
RUN dotnet build "SduHealthier.Cli.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SduHealthier.Cli.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SduHealthier.Cli.dll"]
