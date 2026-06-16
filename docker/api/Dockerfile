FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AwardsFerm.slnx ./
COPY src/AwardsFerm.Core/AwardsFerm.Core.csproj src/AwardsFerm.Core/
COPY src/AwardsFerm.Api/AwardsFerm.Api.csproj src/AwardsFerm.Api/
RUN dotnet restore src/AwardsFerm.Api/AwardsFerm.Api.csproj
COPY src/AwardsFerm.Core/ src/AwardsFerm.Core/
COPY src/AwardsFerm.Api/ src/AwardsFerm.Api/
RUN dotnet publish src/AwardsFerm.Api/AwardsFerm.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AwardsFerm.Api.dll"]
