FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

COPY *.sln .
COPY client/twitter.csproj ./client/
COPY client.tests/twitter.tests.csproj ./client.tests/
RUN dotnet restore
COPY . .

RUN dotnet test --filter FullyQualifiedName\!~StorageClient_Tests

WORKDIR /source/client
RUN dotnet publish -c release -o /app --self-contained false --no-restore

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim-amd64
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "./twitter.dll"]
