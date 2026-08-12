#!/usr/bin/env bash
set -euo pipefail

PUBLIC_KEY_PATH="${1:-build/public.key}"
LICENSE_PATH="${2:-build/result.gitlab-license}"

GITLAB_HOST="${GITLAB_HOST:-${GITLAB_DEPLOY_HOST:-}}"
GITLAB_USER="${GITLAB_USER:-root}"
GITLAB_SSH_PORT="${GITLAB_SSH_PORT:-22}"
GITLAB_CONTAINER_NAME="${GITLAB_CONTAINER_NAME:-gitlab}"
GITLAB_API_URL="${GITLAB_API_URL:-}"
GITLAB_PRIVATE_TOKEN="${GITLAB_PRIVATE_TOKEN:-}"
REMOTE_DIR="/tmp/gitlab-license-$(date +%s)"

if [[ -z "${GITLAB_HOST}" ]]; then
  echo "[!] GITLAB_HOST is required. Set it to the remote GitLab Docker host."
  exit 1
fi

if [[ ! -f "${PUBLIC_KEY_PATH}" ]]; then
  echo "[!] public key not found: ${PUBLIC_KEY_PATH}"
  exit 1
fi

if [[ ! -f "${LICENSE_PATH}" ]]; then
  echo "[!] license file not found: ${LICENSE_PATH}"
  exit 1
fi

SSH_CONFIG_OPTS=(
  -o StrictHostKeyChecking=yes
  -o UserKnownHostsFile="${HOME}/.ssh/known_hosts"
  -o IdentitiesOnly=yes
)

echo "[*] uploading to ${GITLAB_USER}@${GITLAB_HOST}:${REMOTE_DIR}"
ssh "${SSH_CONFIG_OPTS[@]}" -p "${GITLAB_SSH_PORT}" "${GITLAB_USER}@${GITLAB_HOST}" "mkdir -p '${REMOTE_DIR}'"
scp "${SSH_CONFIG_OPTS[@]}" -P "${GITLAB_SSH_PORT}" "${PUBLIC_KEY_PATH}" "${LICENSE_PATH}" "${GITLAB_USER}@${GITLAB_HOST}:${REMOTE_DIR}/"

ssh "${SSH_CONFIG_OPTS[@]}" -p "${GITLAB_SSH_PORT}" "${GITLAB_USER}@${GITLAB_HOST}" "bash -s -- '${REMOTE_DIR}' '${GITLAB_CONTAINER_NAME}' '${GITLAB_API_URL}' '${GITLAB_PRIVATE_TOKEN}'" <<'REMOTE'
#!/usr/bin/env bash
set -euo pipefail

remote_dir="$1"
container_name="$2"
gitlab_api_url="$3"
private_token="$4"

if sudo docker ps --format '{{.Names}}' | grep -qxF "${container_name}"; then
  sudo docker cp "${remote_dir}/public.key" "${container_name}:/opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub"
  sudo docker exec "${container_name}" bash -lc 'gitlab-ctl reconfigure && gitlab-ctl restart'
else
  sudo mkdir -p /opt/gitlab/embedded/service/gitlab-rails
  sudo install -m 0644 "${remote_dir}/public.key" /opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub
  sudo gitlab-ctl reconfigure
  sudo gitlab-ctl restart
fi

if [[ -n "${gitlab_api_url}" && -n "${private_token}" ]]; then
  curl --fail --silent --show-error \
    -X POST "${gitlab_api_url}/api/v4/license" \
    -H "PRIVATE-TOKEN: ${private_token}" \
    -F "license=@${remote_dir}/result.gitlab-license"
fi
REMOTE

echo "[*] GitLab license deployment completed"
