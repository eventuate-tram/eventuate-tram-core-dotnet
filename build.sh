#!/usr/bin/env bash

set -e
set -x

VERSION_ARGS=()
# If this is not a tagged release, set version suffix to 'preview-{run_number}'
# Otherwise we will use the version as it is defined in the project file
if [[ ${IS_TAGGED_RELEASE} != 'true' ]] ; then
    VERSION_SUFFIX=preview-${GITHUB_RUN_NUMBER:-local}
    VERSION_ARGS=("--version-suffix" "${VERSION_SUFFIX}")
fi

dotnet build -c Release ${VERSION_ARGS[@]}

dotnet pack -c Release --no-build ${VERSION_ARGS[@]} -p:PackageId=${PACKAGE_ID:-IO.Eventuate.Tram} IO.Eventuate.Tram
