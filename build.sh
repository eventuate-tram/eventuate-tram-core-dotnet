#!/usr/bin/env bash

set -e
set -x

# Always a preview version for now
IS_PREVIEW=true

VERSION_PREFIX='""'
if [[ ${IS_PREVIEW} = true ]] ; then
    VERSION_PREFIX=preview-${GITHUB_RUN_NUMBER}
fi

dotnet build -c Release --version-suffix ${VERSION_PREFIX}

dotnet pack -c Release --version-suffix ${VERSION_PREFIX}
