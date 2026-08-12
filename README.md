<div align="center">

# GitLab License Generator

<p align="center">
  <a href="README.md">English</a> |
  <a href="lang/README_FR.md">Français</a> |
  <a href="lang/README_RU.md">Russian</a>
</p>

</div>

## Description

**GitLab License Generator** This project generates a GitLab license for **development purposes**. If you encounter any problems, please troubleshoot them on your own.

> Last tested on GitLab EE v19.3.0.

## Principles

### **src/generator.keys.rb**

GitLab uses a public/private key pair to encrypt its license. The public key is shipped with the GitLab distribution, while the private key is kept secure. The license itself is simply a JSON dictionary. Since GitLab has made its code open-source, we can easily generate our own license.

### **src/generator.license.rb**

The `lib` folder is extracted from GitLab's source code. It is used to build and validate the license. The script `src/generator.license.rb` loads this functionality.

### **src/scan.features.rb**

Features are extracted from an object filled with constants. The most comprehensive plan for a license is **Ultimate**, but features like Geo Mirroring are not included in any standard plan. Therefore, we manually add these features.

## Usage

### GitHub Actions: generate and deploy on demand

This repository includes a manual GitHub Actions workflow in `.github/workflows/build.yml`. It can be triggered from the GitHub UI and will:

- generate a GitLab EE license with your selected metadata
- upload the public key and license file as workflow artifacts
- optionally push the generated files to a remote GitLab Docker host over SSH and upload the license with the GitLab API

Important security notes:

- `GITLAB_HOME` must be set on the GitLab Docker host before starting the container.
- The private key should never be published as a long-lived artifact in GitHub; keep it local or in a protected secret store.
- The SSH deployment verifies the remote host key via `ssh-keyscan` before connecting.

Required repository secrets for deployment:

- `GITLAB_SSH_PRIVATE_KEY`: private SSH key for the GitLab Docker host
- `GITLAB_ADMIN_TOKEN`: GitLab administrator personal access token with API access

For Docker-based GitLab hosts, the expected minimum setup is:

```bash
export GITLAB_HOME=/srv/gitlab
mkdir -p "$GITLAB_HOME/config" "$GITLAB_HOME/logs" "$GITLAB_HOME/data"
```

Example manual run values:

```yaml
license_name: Tim Cook
license_company: Apple Computer, Inc.
license_email: tcook@apple.com
license_plan: ultimate
license_user_count: 2147483647
license_expire_year: 2500
gitlab_version: 19.3.0
deploy: true
gitlab_host: gitlab.example.com
gitlab_user: root
gitlab_ssh_port: '22'
gitlab_container_name: gitlab
gitlab_api_url: https://gitlab.example.com
```

The deployment helper script is `bash ./scripts/deploy_gitlab_license.sh`.

### Using Docker image (Zero setup)

Using this method license files are generated under `./license` directory
> Please note that in standard docker installations, owner of the files generated in license directory will be root

#### Method (1): Pull image

```bash
docker run --rm -it \
  -v "./license:/license-generator/build" \
  -e LICENSE_NAME="Tim Cook" \
  -e LICENSE_COMPANY="Apple Computer, Inc." \
  -e LICENSE_EMAIL="tcook@apple.com" \
  -e LICENSE_PLAN="ultimate" \
  -e LICENSE_USER_COUNT="2147483647" \
  -e LICENSE_EXPIRE_YEAR="2500" \
  ghcr.io/lakr233/gitlab-license-generator:main
```

#### Method (2): Build image

```bash
git clone https://github.com/Lakr233/GitLab-License-Generator.git
docker build GitLab-License-Generator -t gitlab-license-generator:main
docker run --rm -it \
  -v "./license:/license-generator/build" \
  -e LICENSE_NAME="Tim Cook" \
  -e LICENSE_COMPANY="Apple Computer, Inc." \
  -e LICENSE_EMAIL="tcook@apple.com" \
  -e LICENSE_PLAN="ultimate" \
  -e LICENSE_USER_COUNT="2147483647" \
  -e LICENSE_EXPIRE_YEAR="2500" \
  gitlab-license-generator:main
```

### Manual: Prerequisites

Before starting, ensure your environment is properly configured.

#### 1. Install Ruby and gem

To run this project, you need **Ruby** and the **gem** package manager.

- **On Linux (Ubuntu/Debian)**:

  ```bash
  sudo apt update
  sudo apt install ruby-full
  ```

- **On macOS** (via Homebrew):

  ```bash
  brew install ruby
  ```

#### 2. Install Bundler and necessary gems

After installing Ruby, you need to install **Bundler** to manage Ruby dependencies.

```bash
gem install bundler
```

#### 3. Install the `gitlab-license` gem

The project requires the `gitlab-license` gem, which will be automatically downloaded and used by the script.

```bash
gem install gitlab-license
```

### Steps to Generate the GitLab License

#### 1. Clone the project repository

Clone this project to your local machine.

```bash
git clone https://github.com/Lakr233/GitLab-License-Generator.git
cd GitLab-License-Generator
```

#### 2. Run the `make.sh` script

Once all the prerequisites are met, run the script:

```bash
./make.sh
```

The script will perform the following actions:

- Download and extract the `gitlab-license` gem.
- Copy and modify the required files.
- Clone the GitLab source code from GitLab.com.
- Generate a public/private key pair.
- Generate a GitLab license.

#### 3. Replace the public key in GitLab

The script generates a public key located in `build/public.key`. You need to replace GitLab’s existing public key with this newly generated one to ensure the license is accepted.

- **If GitLab is installed on your server**:

  ```bash
  sudo cp ./build/public.key /opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub
  sudo gitlab-ctl reconfigure
  sudo gitlab-ctl restart
  ```

- **If GitLab is installed via Docker**:
  Set `GITLAB_HOME` and use the provided Compose example:

  ```bash
  export GITLAB_HOME=/srv/gitlab
  mkdir -p "$GITLAB_HOME/config" "$GITLAB_HOME/logs" "$GITLAB_HOME/data"
  docker compose -f docker-compose.gitlab-ee-19.3.yml up -d
  ```

  Then place the generated public key into the container or use the deployment helper:

  ```bash
  docker cp ./build/public.key gitlab:/opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub
  docker exec gitlab gitlab-ctl reconfigure
  docker exec gitlab gitlab-ctl restart
  ```

#### 4. Install the license in GitLab

Once the public key is replaced, log in to GitLab’s admin interface to install the generated license.

1. Log in to GitLab as an administrator.
2. Navigate to the **Admin Area** from the bottom-left corner.
3. Go to **Settings > General** and upload the generated license file (`build/result.gitlab-license`).
4. Check the **Terms of Service** checkbox and click **Add License**.

If necessary, you can directly access the license upload page via:

```
<YourGitLabURL>/admin/license/new
```

#### 5. Disable Service Ping (optional)

If you want to disable GitLab’s usage data collection (Service Ping), modify GitLab’s configuration file:

- Open the configuration file:

  ```bash
  sudo nano /etc/gitlab/gitlab.rb
  ```

- Add the following line:

  ```bash
  gitlab_rails['usage_ping_enabled'] = false
  ```

- Reconfigure and restart GitLab:

  ```bash
  sudo gitlab-ctl reconfigure
  sudo gitlab-ctl restart
  ```

### Troubleshooting

- **HTTP 502 Error**:
  If you encounter this error, wait for GitLab to finish starting up (it may take some time).

## LICENSE

This project is licensed under the **WTFPL License**.

Copyright (c) 2023, Tim Cook, All Rights Not Reserved.
