#!/bin/bash

PROJECT_PATH="EBA.sln"
OUTPUT_BASE="./builds"

PLATFORMS=("win-x64" "osx-x64" "osx-arm64" "linux-x64")

echo "Starting production build for $PROJECT_PATH..."

rm -rf "$OUTPUT_BASE"
mkdir -p "$OUTPUT_BASE"

for RID in "${PLATFORMS[@]}"
do
  echo "---------------------------------------------------"
  echo "Publishing for: $RID..."  

  TARGET_DIR="$OUTPUT_BASE/$RID"

  dotnet publish "$PROJECT_PATH" \
    -c Release \
    -r "$RID" \
    --output "$TARGET_DIR" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:TrimMode=CopyUsed \
    -p:EnableCompressionInSingleFile=true

  if [ -d "$TARGET_DIR" ]; then
    echo "Compressing $RID..."
    
    # using subshell to jump in, zip, and jump out without changing the script's WD
    # The zip is placed one level up (in ./builds/)
    (cd "$TARGET_DIR" && zip -q -r "../${RID}.zip" .)
    
    echo "Done: ${RID}.zip and /$RID/ folder are ready."
  else
    echo "Error: Build failed for $RID."
  fi
done

echo "---------------------------------------------------"
echo "Build process complete. Everything is in $OUTPUT_BASE"
