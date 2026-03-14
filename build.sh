#!/bin/bash

PROJ="EBA/EBA.csproj"
OUT="./.builds"
RIDS=("win-x64" "osx-x64" "osx-arm64" "linux-x64")

# If $1 is "-h", show RIDs. If $1 is a RID, use it. Otherwise, use ALL.
[[ "$1" == "-h" ]] && echo "Platforms: ${RIDS[*]}" && exit

# Since we dropped -w, we can just use $1 directly as the target.
TARGETS=${1:-"${RIDS[@]}"}

# Always show warnings (Minimal verbosity shows errors and warnings)
WARN="--verbosity minimal"

echo "Building: $TARGETS"

for RID in $TARGETS; do
  DIR="$OUT/$RID"
  
  echo "Processing $RID ..."
  rm -rf "$DIR"

  # dotnet publish with warnings enabled
  dotnet publish "$PROJ" \
    -c Release \
    -r "$RID" \
    -o "$DIR" \
    --self-contained true \
    $WARN \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false || { echo "Build failed for $RID"; exit 1; }

  # Zip the output and move up to the .builds folder
  (cd "$DIR" && zip -q -r "../$RID.zip" .)
done