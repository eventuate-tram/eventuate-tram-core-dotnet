FROM mcr.microsoft.com/dotnet/core/sdk:3.1
WORKDIR /app

ENTRYPOINT [ "dotnet", "vstest", "IO.Eventuate.Tram.IntegrationTests.dll", "/logger:trx" ]
