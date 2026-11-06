#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

dotnet build EleanorRomance.csproj -c Release

STAGE="bin/Release"
MOD_DIR="../Mods/EleanorRomance"

test -f "$STAGE/EleanorRomance.dll"
test -s Localization/zh.csv
test -s Localization/en.csv

# Keep a rollback copy of the currently deployed build before replacing it.
if [ -d "$MOD_DIR" ]; then
  BACKUP="dist/rollback_$(date +%Y%m%d-%H%M%S)"
  mkdir -p "$BACKUP"
  cp -r "$MOD_DIR"/. "$BACKUP"/ 2>/dev/null || true
  echo "Rollback copy: $BACKUP"
fi

rm -rf "$MOD_DIR/Localization" "$MOD_DIR/Textures" "$MOD_DIR/Books"
mkdir -p "$MOD_DIR/Localization" "$MOD_DIR/Textures/Items"
cp manifest.xml "$MOD_DIR/manifest.xml"
cp "$STAGE/EleanorRomance.dll" "$MOD_DIR/EleanorRomance.dll"
cp "$STAGE/EleanorRomance.deps.json" "$MOD_DIR/EleanorRomance.deps.json" 2>/dev/null || true
cp Localization/*.csv "$MOD_DIR/Localization/"
cp Textures/Items/*.png "$MOD_DIR/Textures/Items/" 2>/dev/null || true
cp preview.png "$MOD_DIR/preview.png" 2>/dev/null || true
cp README.md "$MOD_DIR/README.md" 2>/dev/null || true

# Fail loudly if the deployed text is empty or the DLL is missing.
test -s "$MOD_DIR/EleanorRomance.dll"
test -s "$MOD_DIR/Localization/zh.csv"
test -s "$MOD_DIR/Localization/en.csv"
echo "Built into $MOD_DIR"
