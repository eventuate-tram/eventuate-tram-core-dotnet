#!/usr/bin/env bash

set -e

echo Publishing tagged release nuget package
dotnet nuget push IO.Eventuate.Tram/bin/Release/*.nupkg --source ${NUGET_PUBLISH_URL} --api-key ${NUGET_API_KEY}
