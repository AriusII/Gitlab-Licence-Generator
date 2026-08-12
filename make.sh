#!/usr/bin/env bash
# encoding: utf-8

set -euo pipefail

echo "[i] GitLab License Generator"
echo "[i] Copyright (c) 2023 Tim Cook, All Rights Not Reserved"

LICENSE_NAME="${LICENSE_NAME:-"Tim Cook"}"
LICENSE_COMPANY="${LICENSE_COMPANY:-"Apple Computer, Inc."}"
LICENSE_EMAIL="${LICENSE_EMAIL:-"tcook@apple.com"}"
LICENSE_PLAN="${LICENSE_PLAN:-ultimate}"
LICENSE_USER_COUNT="${LICENSE_USER_COUNT:-2147483647}"
LICENSE_EXPIRE_YEAR="${LICENSE_EXPIRE_YEAR:-2500}"
GITLAB_SOURCE_REF="${GITLAB_SOURCE_REF:-${GITLAB_VERSION:-"master"}}"

declare -a GITLAB_REF_CANDIDATES=()
if [[ -n "${GITLAB_SOURCE_REF}" ]]; then
  GITLAB_REF_CANDIDATES+=("${GITLAB_SOURCE_REF}")
  if [[ "${GITLAB_SOURCE_REF}" != v* ]]; then
    GITLAB_REF_CANDIDATES+=("v${GITLAB_SOURCE_REF}")
  fi
  if [[ "${GITLAB_SOURCE_REF}" != *"-ee"* ]]; then
    GITLAB_REF_CANDIDATES+=("${GITLAB_SOURCE_REF}-ee")
    if [[ "${GITLAB_SOURCE_REF}" != v* ]]; then
      GITLAB_REF_CANDIDATES+=("v${GITLAB_SOURCE_REF}-ee")
    fi
  fi
fi
GITLAB_REF_CANDIDATES+=("master" "main")
GITLAB_SOURCE_REF=""

for candidate in "${GITLAB_REF_CANDIDATES[@]}"; do
  if [[ -z "$candidate" ]]; then
    continue
  fi
  FEATURES_SOURCE_URL="https://gitlab.com/gitlab-org/gitlab/-/raw/${candidate}/ee/app/models/gitlab_subscriptions/features.rb?inline=false"
  if curl -fsSL "$FEATURES_SOURCE_URL" >/dev/null 2>&1; then
    GITLAB_SOURCE_REF="$candidate"
    break
  fi
done

if [[ -z "$GITLAB_SOURCE_REF" ]]; then
  echo "[!] failed to resolve a valid GitLab feature source ref from: ${GITLAB_REF_CANDIDATES[*]}"
  exit 1
fi

cd "$(dirname "$0")"
if [ ! -f ".root" ]; then
  echo "[!] failed to locate project directory, aborting..."
  exit 1
fi
WORKING_DIR=$(pwd)

mkdir -p temp

if [ ! -f "./src/scan.features.rb" ]; then
  echo "[!] src/scan.features.rb is missing, aborting..."
  exit 1
fi
chmod +x ./src/scan.features.rb 2>/dev/null || true

echo "[*] fetching ruby gem version..."
RB_GEM_NAME="gitlab-license"
RB_GEM_LIST_OUTPUT=$(gem list --remote "$RB_GEM_NAME")

RB_GEM_VERSION=""
while IFS= read -r line; do
  if [[ $line == *"gitlab-license ("* ]]; then
    RB_GEM_VERSION="${line#*"gitlab-license ("}"
    RB_GEM_VERSION="${RB_GEM_VERSION%%)*}"
    break
  fi
done <<<"$RB_GEM_LIST_OUTPUT"

if [[ -z "$RB_GEM_VERSION" ]]; then
  echo "[!] failed to discover gitlab-license gem version, aborting..."
  exit 1
fi

echo "[*] gitlab-license version: $RB_GEM_VERSION"
RB_GEM_DOWNLOAD_URL="https://rubygems.org/downloads/gitlab-license-$RB_GEM_VERSION.gem"
RB_GEM_DOWNLOAD_PATH=$(pwd)/temp/gem/gitlab-license.gem
mkdir -p "$(dirname "$RB_GEM_DOWNLOAD_PATH")"
curl -fsSL "$RB_GEM_DOWNLOAD_URL" -o "$RB_GEM_DOWNLOAD_PATH"
pushd "$(dirname "$RB_GEM_DOWNLOAD_PATH")" >/dev/null
tar -xf gitlab-license.gem
tar -xf data.tar.gz

if [ ! -f "./lib/gitlab/license.rb" ]; then
  echo "[!] failed to locate gem file, aborting..."
  exit 1
fi

echo "[*] copying gem..."
rm -rf "${WORKING_DIR:?}/lib" || true
mkdir -p "$WORKING_DIR/lib"
cp -r ./lib/gitlab/* "$WORKING_DIR/lib"
popd >/dev/null

pushd lib >/dev/null
echo "[*] patching lib requirements gem..."

OS_TYPE="$(uname -s)"
case "$OS_TYPE" in
  Linux*|MINGW*|MSYS*|CYGWIN*)
    sed_i_cmd="sed -i"
    ;;
  Darwin*)
    sed_i_cmd="sed -i ''"
    ;;
  *)
    echo "Unsupported OS: $OS_TYPE"
    exit 1
    ;;
esac

find . -type f -exec $sed_i_cmd 's/require '\''gitlab\/license\//require_relative '\''license\//g' {} \;

popd >/dev/null

echo "[*] updated gem"

echo "[*] fetching gitlab source code..."
GITLAB_SOURCE_CODE_DIR=$(pwd)/temp/src
mkdir -p "$GITLAB_SOURCE_CODE_DIR"
FEATURES_SOURCE_URL="https://gitlab.com/gitlab-org/gitlab/-/raw/${GITLAB_SOURCE_REF}/ee/app/models/gitlab_subscriptions/features.rb?inline=false"

echo "[*] using GitLab ref ${GITLAB_SOURCE_REF}"
if ! curl -fsSL "$FEATURES_SOURCE_URL" -o "$GITLAB_SOURCE_CODE_DIR/features.rb"; then
  echo "[!] failed to fetch feature definitions for GitLab ref ${GITLAB_SOURCE_REF}"
  exit 1
fi

BUILD_DIR=$(pwd)/build
mkdir -p "$BUILD_DIR"

echo "[*] scanning features..."
FEATURE_LIST_FILE=$BUILD_DIR/features.json
rm -f "${FEATURE_LIST_FILE:?}" || true
./src/scan.features.rb \
  -o "$FEATURE_LIST_FILE" \
  -f "$GITLAB_SOURCE_CODE_DIR/features.rb"

echo "[*] generating key pair..."
PUBLIC_KEY_FILE=$BUILD_DIR/public.key
PRIVATE_KEY_FILE=$BUILD_DIR/private.key
cp -f ./keys/public.key "$PUBLIC_KEY_FILE"
cp -f ./keys/private.key "$PRIVATE_KEY_FILE"

# execute following command to generate new keys
# ./src/generator.keys.rb \
#     --public-key $PUBLIC_KEY_FILE \
#     --private-key $PRIVATE_KEY_FILE

echo "[*] generating license..."
LICENSE_FILE=$BUILD_DIR/result.gitlab-license
LICENSE_JSON_FILE=$BUILD_DIR/license.json

./src/generator.license.rb \
  -f "$FEATURE_LIST_FILE" \
  --public-key "$PUBLIC_KEY_FILE" \
  --private-key "$PRIVATE_KEY_FILE" \
  -o "$LICENSE_FILE" \
  --license-name "$LICENSE_NAME" \
  --license-company "$LICENSE_COMPANY" \
  --license-email "$LICENSE_EMAIL" \
  --license-plan "$LICENSE_PLAN" \
  --license-user-count "$LICENSE_USER_COUNT" \
  --license-expire-year "$LICENSE_EXPIRE_YEAR" \
  --plain-license "$LICENSE_JSON_FILE"

echo "[*] done $(basename "$0")"
