#!/bin/bash

PROJ="EBA/EBA.csproj"
OUT="./.builds"
RIDS=("win-x64" "osx-x64" "osx-arm64" "linux-x64")

# If $1 is "-h", show RIDs. If $1 is a RID, use it. Otherwise, use ALL.
[[ "$1" == "-h" ]] && echo "Platforms: ${RIDS[*]}" && exit
TARGETS=${1:-"${RIDS[@]}"}

# Set WARN="" to see warnings. Default is quiet.
WARN="-p:NoWarn=0000-9999 --verbosity quiet"
[[ "$*" == *"-w"* ]] && WARN=""

echo "Building: $TARGETS"

for RID in $TARGETS; do
  [[ "$RID" == "-w" ]] && continue

  DIR="$OUT/$RID"
  rm -rf "$DIR"

  dotnet publish "$PROJ" -c Release -r "$RID" -o "$DIR" \
    --self-contained true $WARN \
    -p:PublishSingleFile=true -p:PublishTrimmed=true || exit 1

  (cd "$DIR" && zip -q -r "../$RID.zip" .)
  echo "✓ Created $RID.zip"
done
