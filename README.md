<div align="center">

# GitLab License Generator

<p align="center">
  <a href="README.md">English</a> |
  <a href="lang/README_FR.md">Français</a> |
  <a href="lang/README_RU.md">Russian</a>
</p>

</div>

## What this is

A tool that generates a self-signed **GitLab EE Ultimate** license, for **local/development use only**.

It does exactly one thing: produce an RSA key pair and a `.gitlab-license` file granting the maximum permissions an offline license can carry — every EE Ultimate feature, every GitLab Duo / seat-based add-on, effectively unlimited users, an expiry far in the future.

**What it does not do**: it never connects to, deploys to, or configures a running GitLab instance. Wiring the generated key/license into your own GitLab deployment is a manual step you do yourself (see [Install the license](#install-the-license), or the `docker-compose` example below).

**Works out of the box**: a ready-to-use RSA key pair (`keys/`) ships committed in this repository, so cloning/forking it is enough to generate a working license immediately — no setup step required. That pair is therefore identical across every clone and fork; see the security note under [Configuration](#configuration) if that matters for your use case.

**Configuration is settings-only, not CLI flags.** Everything is controlled by `appsettings.json` (bundled with the tool) and, for automation, environment variables. There are no command-line options to learn.

> Feature/add-on set verified against gitlab-org/gitlab's `ee/app/models/gitlab_subscriptions/features.rb` (master / 19.x line).

## Get a license

### Option A — Docker Compose (recommended)

```bash
git clone https://github.com/Lakr233/GitLab-License-Generator.git
cd GitLab-License-Generator
docker compose up --build
```

This builds the image (which already includes the repository's default key pair) and writes the license to `./output/result.gitlab-license`. Every run also copies the exact public key that signed it (`public.key`) into `./output/` — that's the only key material GitLab itself needs, so the license and the key to install it live together in one directory. The private key stays only under `./keys/`. The container runs once and exits — re-run `docker compose up` any time you want to regenerate.

To change any setting, edit the `environment:` block in `docker-compose.yml`:

```yaml
services:
  glgen:
    build: .
    volumes:
      - ./keys:/license-generator/keys
      - ./output:/license-generator/output
    environment:
      License__Name: "Tim Cook"
      License__Company: "Apple Computer, Inc."
      License__Email: "tcook@apple.com"
      License__Plan: "ultimate"
      License__UserCount: "2147483647"
      License__ExpireYear: "2500"
      License__IncludeAddOns: "true"
```

### Option B — plain `docker run`

```bash
docker run --rm \
  -v "./keys:/license-generator/keys" \
  -v "./output:/license-generator/output" \
  -e License__ExpireYear="2500" \
  ghcr.io/lakr233/gitlab-license-generator:main
```

Without mounting `./keys`, the container just uses its baked-in default key pair (deterministic, same every run). Mount `./keys` only if you want to persist a *regenerated* pair of your own across runs (see `RegenerateKeys` below) — otherwise it's optional. Either way, every run copies the public key it used into `./output/` next to the generated license — that's the only key material GitLab itself needs; the private key stays under `./keys/`.

### Option C — from source (.NET 10 SDK required)

```bash
git clone https://github.com/Lakr233/GitLab-License-Generator.git
cd GitLab-License-Generator
dotnet run --project src/GitlabLicenseGenerator.Cli
```

Same result as the Docker options: a key pair under `keys/` and a license under `output/`, using the repository root as the working directory. Every run also copies the public key into `output/` alongside the license — the private key stays under `keys/`.

### Option D — GitHub Actions (no install required)

Generate a license from your browser, with no local .NET or Docker setup:

1. `workflow_dispatch` requires write access to the repository, so if you're not a collaborator here, **fork this repository first**, then open the **Actions** tab on your fork (enable workflows if prompted — a one-time step on a fresh fork).
2. Open **Actions > CI/CD > Run workflow**.
3. Fill in whichever fields you want to change — name, company, email, plan, user count, expiry year, add-ons, or check "regenerate keys" for your own unique key pair instead of the shared default one — and leave the rest at their defaults.
4. Run it, then download the `gitlab-license` artifact from the finished run: it contains the license, its plaintext JSON, and the matching public key. **Download it right away** — it expires after 1 day (GitHub's minimum retention), same as the daily scheduled run below.

This same workflow also runs automatically every day at 09:00 UTC with default settings — that scheduled run exercises the pipeline, it isn't a distribution channel, so use steps 1–4 above for a license you actually intend to install.

## Configuration

All settings live in the `License` section of `appsettings.json` (`src/GitlabLicenseGenerator.Cli/appsettings.json`), bundled next to the executable. You never need to edit it to get a working license — every default already grants maximum permissions.

| Setting | Default | Description |
| --- | --- | --- |
| `Name` | `Tim Cook` | Licensee name |
| `Company` | `Apple Computer, Inc.` | Licensee company |
| `Email` | `tcook@apple.com` | Licensee email |
| `Plan` | `ultimate` | `ultimate`, `premium`, or `starter` |
| `UserCount` | `2147483647` | Active user / seat count |
| `ExpireYear` | `2500` | License expiry year (month/day are always April 1st) |
| `IncludeAddOns` | `true` | Also grant every GitLab Duo / seat-based add-on |
| `PublicKeyPath` | `keys/public.key` | Where the RSA public key is read from/written to — the repository ships a default pair here |
| `PrivateKeyPath` | `keys/private.key` | Where the RSA private key is read from/written to — the repository ships a default pair here |
| `RegenerateKeys` | `false` | Set `true` once to replace the shared default pair with a fresh, private one of your own |
| `OutputPath` | `output/result.gitlab-license` | Where the encrypted license file is written (the public key used is copied alongside it — GitLab never needs the private key) |
| `PlainLicensePath` | `output/license.json` | Optional plaintext copy of the license JSON, for inspection |

To override a setting without editing the file, set an environment variable using .NET's standard double-underscore convention for nested keys — this is how every example above (and the Docker image) does it:

```bash
export License__Name="Ada Lovelace"
export License__ExpireYear=2100
```

### Security note: the shared default key pair

`keys/private.key` and `keys/public.key` are committed to this repository so it works immediately after a clone/fork, with no generation step. That also means **every clone and fork shares the exact same key pair** — anyone can decrypt/re-sign a `.gitlab-license` file with it. In practice this is a low-risk trade-off for this tool's actual use case: installing a custom public key on a GitLab instance already requires root/admin access to that instance, so the shared pair doesn't hand out anything an attacker wouldn't already need higher privileges to obtain. It is still meant for **local/development use only** — if you want a key pair only you have, delete `keys/private.key` and `keys/public.key` (or set `License__RegenerateKeys=true` for one run) before generating the license you'll actually install. Every run also copies whichever public key it used into the (gitignored) `output/` directory alongside the license — that's a local convenience copy of the same public key described above, not a new or additional one, so it doesn't change the posture. The private key is never copied out of `keys/`.

## Install the license

Once you have `keys/public.key` and `output/result.gitlab-license`:

### 1. Replace GitLab's license encryption public key

GitLab reads its license encryption public key from a fixed path inside the instance: `/opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub`. You must overwrite it with the public key this tool generated, or the license you install in step 2 will be rejected. (`output/public.key` from the same generation run is an identical copy and works interchangeably with `keys/public.key` below.)

**Docker / docker-compose** — mount it read-only in your own `docker-compose.yml` (see `examples/docker-compose.gitlab-ee-19.3.yml` for a full example):

```yaml
services:
  gitlab:
    image: gitlab/gitlab-ee:19.3.0-ee.0
    volumes:
      - ./keys/public.key:/opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub:ro
      # ...your other GitLab volumes/config
```

Then reconfigure once the container is up:

```bash
docker exec gitlab gitlab-ctl reconfigure
docker exec gitlab gitlab-ctl restart
```

**Bare-metal / Omnibus install**:

```bash
sudo cp ./keys/public.key /opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub
sudo gitlab-ctl reconfigure
sudo gitlab-ctl restart
```

### 2. Upload the license in GitLab

1. Log in to GitLab as an administrator.
2. Go to **Admin Area > Settings > General**.
3. Find the **"Add License"** section, upload `output/result.gitlab-license`, check the Terms of Service checkbox, and click **Add License**.
4. GitLab redirects you to **Admin Area > Subscription**, where you can review the installed license (and, if it includes add-ons, the GitLab Duo seats it granted) at any time.

> There is no `/admin/license/new` page in current GitLab — the upload form lives on the General settings page above.

### Optional: disable Service Ping

```bash
sudo nano /etc/gitlab/gitlab.rb
```

```ruby
gitlab_rails['usage_ping_enabled'] = false
```

```bash
sudo gitlab-ctl reconfigure
sudo gitlab-ctl restart
```

## Testing against a local GitLab instance

`examples/docker-compose.gitlab-ee-19.3.yml` is a reference GitLab EE server pre-wired to accept a license from this tool (it already mounts `../keys/public.key` into the right path). It is **not** part of the generator — it's provided purely so you have something to test the generated license against:

```bash
export GITLAB_HOME=/srv/gitlab
mkdir -p "$GITLAB_HOME/config" "$GITLAB_HOME/logs" "$GITLAB_HOME/data"
docker compose -f examples/docker-compose.gitlab-ee-19.3.yml up -d
```

## How it works

- **`GitlabLicenseGenerator.Core/Crypto`** — RSA key generation and the license encryption envelope (GitLab encrypts its license JSON with an RSA/AES scheme; the public half ships with GitLab, the private half stays with whoever issues licenses).
- **`GitlabLicenseGenerator.Core/Licensing`** — builds, validates, serializes and exports the license:
  - `LicenseFeatureCatalog` — every EE Ultimate feature symbol GitLab's own `FEATURES_BY_PLAN` maps to the `ultimate` plan.
  - `LicenseAddOnCatalog` / `LicenseAddOnPurchase` — the seat-based add-on mechanism (`restrictions.add_on_products`) granting GitLab Duo Pro, Duo Enterprise, Duo with Amazon Q, Duo Core, Duo Agent Platform Self-Hosted, GitLab Credits, Secrets Manager and Flex Offline. This only provisions on an **offline cloud license** (`cloud_licensing_enabled` + `offline_cloud_licensing_enabled`, both always set by this tool).
  - `LicenseFactory` — combines the above into one maximal-permissions `License`.
  - `LicenseValidator` / `LicenseCodec` / `LicenseJsonConverter` — validation, GitLab-compatible JSON shape, encryption/export.

## Troubleshooting

- **HTTP 502 from GitLab**: wait for it to finish starting up — it can take several minutes on first boot.
- **License rejected**: confirm you replaced `.license_encryption_key.pub` with the *matching* `keys/public.key` from the same generation run you're uploading, and restarted GitLab after copying it.

## LICENSE

This project is licensed under the **WTFPL License**.

Copyright (c) 2023, Tim Cook, All Rights Not Reserved.
