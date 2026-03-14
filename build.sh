#!/bin/bash

PROJ="EBA/EBA.csproj"
OUT="./.builds"
RIDS=("win-x64" "osx-x64" "osx-arm64" "linux-x64")

[[ "$1" == "-h" ]] && echo "Platforms: ${RIDS[*]}" && exit

TARGETS=${1:-"${RIDS[@]}"}

WARN="--verbosity minimal"

echo "Building: $TARGETS"

for RID in $TARGETS; do
  DIR="$OUT/$RID"
  
  echo "Processing $RID ..."
  rm -rf "$DIR"

  dotnet publish "$PROJ" \
    -c Release \
    -r "$RID" \
    -o "$DIR" \
    --self-contained true \
    $WARN \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false || { echo "Build failed for $RID"; exit 1; }

  (cd "$DIR" && zip -q -r "../$RID.zip" .)
done