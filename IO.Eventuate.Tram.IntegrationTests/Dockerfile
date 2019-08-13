FROM microsoft/dotnet:2.2-sdk
WORKDIR /app

ENTRYPOINT [ "dotnet", "vstest", "IO.Eventuate.Tram.IntegrationTests.dll", "/logger:trx" ]
