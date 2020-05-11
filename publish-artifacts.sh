#!/usr/bin/env bash

set -e

if [[ ${IS_TAGGED_RELEASE} = 'true' ]] ; then
    echo Publishing tagged release nuget package
    dotnet nuget push IO.Eventuate.Tram/bin/Release/*.nupkg --source ${NUGET_PUBLISH_URL} --api-key ${NUGET_API_KEY}
else
    echo Publishing snapshot nuget package
    dotnet nuget push IO.Eventuate.Tram/bin/Release/*.nupkg --source ${SNAPSHOT_NUGET_PUBLISH_URL} --api-key ${SNAPSHOT_NUGET_API_KEY}
fi
