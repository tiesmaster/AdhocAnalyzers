FROM microsoft/dotnet:latest

COPY . /app
WORKDIR /app

RUN ["dotnet", "build", "AdhocAnalyzers.Test/AdhocAnalyzers.Test.csproj", "-f", "netcoreapp2.0"]

RUN ["dotnet", "test", "AdhocAnalyzers.Test/AdhocAnalyzers.Test.csproj", "-f", "netcoreapp2.0"]