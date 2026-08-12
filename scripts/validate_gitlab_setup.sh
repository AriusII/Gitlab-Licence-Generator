#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${GITLAB_HOME:-}" ]]; then
  echo "[!] GITLAB_HOME is not set. Export it before starting GitLab, for example:"
  echo "    export GITLAB_HOME=/srv/gitlab"
  exit 1
fi

for dir in "${GITLAB_HOME}/config" "${GITLAB_HOME}/logs" "${GITLAB_HOME}/data"; do
  mkdir -p "$dir"
  echo "[*] ensured ${dir}"
done

if [[ ! -d "${GITLAB_HOME}/data" || ! -d "${GITLAB_HOME}/logs" || ! -d "${GITLAB_HOME}/config" ]]; then
  echo "[!] GitLab directories were not created successfully"
  exit 1
fi

echo "[*] GitLab Docker data directories are ready under ${GITLAB_HOME}"
