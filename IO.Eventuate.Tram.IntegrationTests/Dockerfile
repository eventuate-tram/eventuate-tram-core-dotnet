FROM microsoft/dotnet:2.1-sdk
WORKDIR /app

ENTRYPOINT [ "dotnet", "vstest", "IO.Eventuate.Tram.IntegrationTests.dll", "/logger:trx" ]
