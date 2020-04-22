#!/usr/bin/env bash

set -e
set -x

# Always a preview version for now
IS_PREVIEW=true

VERSION_PREFIX='""'
if [[ ${IS_PREVIEW} = true ]] ; then
    VERSION_SUFFIX=preview-${GITHUB_RUN_NUMBER:-local}
fi

dotnet build -c Release --version-suffix ${VERSION_SUFFIX}

dotnet pack -c Release --no-build --version-suffix ${VERSION_SUFFIX} -p:PackageId=${PACKAGE_ID:-IO.Eventuate.Tram} IO.Eventuate.Tram
