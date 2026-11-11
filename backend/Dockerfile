FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/SchoolAdmission.sln backend/
COPY backend/SchoolAdmission.API/SchoolAdmission.API.csproj backend/SchoolAdmission.API/
COPY backend/SchoolAdmission.Core/SchoolAdmission.Core.csproj backend/SchoolAdmission.Core/
COPY backend/SchoolAdmission.Infrastructure/SchoolAdmission.Infrastructure.csproj backend/SchoolAdmission.Infrastructure/
COPY backend/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj backend/SchoolAdmission.Tests/
RUN dotnet restore backend/SchoolAdmission.sln

COPY backend/ backend/
RUN dotnet publish backend/SchoolAdmission.API/SchoolAdmission.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SchoolAdmission.API.dll"]
