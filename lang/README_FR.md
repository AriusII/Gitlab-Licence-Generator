<div align="center">

# GitLab License Generator

<p align="center">
  <a href="../README.md">English</a> |
  Français |
  <a href="README_RU.md">Russian</a>
</p>

</div>

## Ce que c'est

Un outil qui génère une licence **GitLab EE Ultimate** autosignée, destinée **uniquement à un usage local/de développement**.

Il ne fait qu'une seule chose : produire une paire de clés RSA et un fichier `.gitlab-license` accordant le maximum de permissions qu'une licence hors ligne peut porter — toutes les fonctionnalités EE Ultimate, tous les add-ons GitLab Duo / basés sur des sièges, un nombre d'utilisateurs pratiquement illimité, une date d'expiration très éloignée dans le futur.

**Ce qu'il ne fait pas** : il ne se connecte jamais à une instance GitLab en cours d'exécution, n'y déploie rien et ne la configure pas. Intégrer la clé/licence générée dans votre propre déploiement GitLab est une étape manuelle que vous effectuez vous-même (voir [Installer la licence](#installer-la-licence), ou l'exemple `docker-compose` ci-dessous).

**Fonctionne immédiatement, sans configuration** : une paire de clés RSA prête à l'emploi (`keys/`) est fournie et commitée dans ce dépôt, de sorte que le cloner ou le forker suffit à générer immédiatement une licence fonctionnelle — aucune étape de configuration n'est nécessaire. Cette paire est donc identique sur chaque clone et chaque fork ; consultez la note de sécurité dans la section [Configuration](#configuration) si cela a une importance pour votre cas d'usage.

**La configuration se fait uniquement via des paramètres, pas via des options en ligne de commande.** Tout est contrôlé par `appsettings.json` (fourni avec l'outil) et, pour l'automatisation, par des variables d'environnement. Il n'y a aucune option de ligne de commande à apprendre.

> Ensemble de fonctionnalités/add-ons vérifié par rapport au fichier `ee/app/models/gitlab_subscriptions/features.rb` de gitlab-org/gitlab (branche master / lignée 19.x).

## Obtenir une licence

### Option A — Docker Compose (recommandé)

```bash
git clone https://github.com/Lakr233/GitLab-License-Generator.git
cd GitLab-License-Generator
docker compose up --build
```

Cela construit l'image (qui inclut déjà la paire de clés par défaut du dépôt) et écrit la licence dans `./output/result.gitlab-license`. Chaque exécution copie également la clé publique exacte qui l'a signée (`public.key`) dans `./output/` — c'est la seule clé dont GitLab a besoin —, de sorte que la licence et la clé permettant de l'installer se retrouvent regroupées dans un même répertoire. La clé privée, elle, reste uniquement sous `./keys/`. Le conteneur s'exécute une fois puis s'arrête — relancez `docker compose up` chaque fois que vous souhaitez régénérer la licence.

Pour modifier un paramètre, éditez le bloc `environment:` dans `docker-compose.yml` :

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

### Option B — `docker run` simple

```bash
docker run --rm \
  -v "./keys:/license-generator/keys" \
  -v "./output:/license-generator/output" \
  -e License__ExpireYear="2500" \
  ghcr.io/lakr233/gitlab-license-generator:main
```

Sans monter `./keys`, le conteneur utilise simplement sa paire de clés par défaut intégrée (déterministe, identique à chaque exécution). Ne montez `./keys` que si vous souhaitez conserver, d'une exécution à l'autre, une paire *régénérée* qui vous soit propre (voir `RegenerateKeys` ci-dessous) — sinon, ce montage est facultatif. Dans tous les cas, chaque exécution copie la clé publique utilisée dans `./output/`, à côté de la licence générée — c'est la seule clé dont GitLab a besoin ; la clé privée reste sous `./keys/`.

### Option C — depuis les sources (SDK .NET 10 requis)

```bash
git clone https://github.com/Lakr233/GitLab-License-Generator.git
cd GitLab-License-Generator
dotnet run --project src/GitlabLicenseGenerator.Cli
```

Même résultat qu'avec les options Docker : une paire de clés sous `keys/` et une licence sous `output/`, en utilisant la racine du dépôt comme répertoire de travail. Chaque exécution copie également la clé publique dans `output/`, à côté de la licence — la clé privée reste sous `keys/`.

### Option D — GitHub Actions (aucune installation requise)

Générez une licence depuis votre navigateur, sans installation locale de .NET ni de Docker :

1. `workflow_dispatch` nécessite un accès en écriture au dépôt : si vous n'êtes pas collaborateur ici, **forkez d'abord ce dépôt**, puis ouvrez l'onglet **Actions** de votre fork (activez les workflows si demandé — une étape à faire une seule fois sur un fork tout neuf).
2. Ouvrez **Actions > CI/CD > Run workflow**.
3. Renseignez les champs que vous souhaitez modifier — nom, société, email, plan, nombre d'utilisateurs, année d'expiration, add-ons, ou cochez « regenerate keys » pour obtenir votre propre paire de clés unique au lieu de la paire par défaut partagée — et laissez les autres à leur valeur par défaut.
4. Lancez le workflow, puis téléchargez l'artefact `gitlab-license` depuis l'exécution terminée : il contient la licence, son JSON en clair, et la clé publique correspondante. **Téléchargez-le immédiatement** — il expire au bout d'1 jour (la rétention minimale de GitHub), comme l'exécution planifiée quotidienne ci-dessous.

Ce même workflow s'exécute aussi automatiquement chaque jour à 09:00 UTC avec les paramètres par défaut — cette exécution planifiée sert à faire tourner le pipeline, ce n'est pas un canal de distribution : utilisez donc les étapes 1 à 4 ci-dessus pour obtenir une licence que vous comptez réellement installer.

## Configuration

Tous les paramètres se trouvent dans la section `License` du fichier `appsettings.json` (`src/GitlabLicenseGenerator.Cli/appsettings.json`), fourni à côté de l'exécutable. Il n'est jamais nécessaire de le modifier pour obtenir une licence fonctionnelle — chaque valeur par défaut accorde déjà le maximum de permissions.

| Paramètre | Valeur par défaut | Description |
| --- | --- | --- |
| `Name` | `Tim Cook` | Nom du titulaire de la licence |
| `Company` | `Apple Computer, Inc.` | Société du titulaire de la licence |
| `Email` | `tcook@apple.com` | Adresse e-mail du titulaire de la licence |
| `Plan` | `ultimate` | `ultimate`, `premium` ou `starter` |
| `UserCount` | `2147483647` | Nombre d'utilisateurs actifs / sièges |
| `ExpireYear` | `2500` | Année d'expiration de la licence (le mois et le jour sont toujours fixés au 1er avril) |
| `IncludeAddOns` | `true` | Accorde également tous les add-ons GitLab Duo / basés sur des sièges |
| `PublicKeyPath` | `keys/public.key` | Emplacement de lecture/écriture de la clé publique RSA — le dépôt fournit une paire par défaut à cet emplacement |
| `PrivateKeyPath` | `keys/private.key` | Emplacement de lecture/écriture de la clé privée RSA — le dépôt fournit une paire par défaut à cet emplacement |
| `RegenerateKeys` | `false` | Définissez `true` une fois pour remplacer la paire par défaut partagée par une paire fraîche qui vous soit propre |
| `OutputPath` | `output/result.gitlab-license` | Emplacement d'écriture du fichier de licence chiffré (la clé publique utilisée y est copiée à côté — GitLab n'a jamais besoin de la clé privée) |
| `PlainLicensePath` | `output/license.json` | Copie en clair optionnelle du JSON de la licence, pour inspection |

Pour surcharger un paramètre sans modifier le fichier, définissez une variable d'environnement en utilisant la convention standard de .NET à double tiret bas pour les clés imbriquées — c'est ainsi que procèdent tous les exemples ci-dessus (ainsi que l'image Docker) :

```bash
export License__Name="Ada Lovelace"
export License__ExpireYear=2100
```

### Note de sécurité : la paire de clés par défaut partagée

`keys/private.key` et `keys/public.key` sont commitées dans ce dépôt afin qu'il fonctionne immédiatement après un clone ou un fork, sans étape de génération. Cela signifie aussi que **chaque clone et chaque fork partagent exactement la même paire de clés** — n'importe qui peut déchiffrer/re-signer un fichier `.gitlab-license` avec elle. En pratique, il s'agit d'un compromis à faible risque pour l'usage réel de cet outil : installer une clé publique personnalisée sur une instance GitLab exige déjà un accès root/administrateur à cette instance, de sorte que la paire partagée ne donne accès à rien qu'un attaquant n'aurait pas déjà besoin de privilèges plus élevés pour obtenir. Cela reste néanmoins destiné **uniquement à un usage local/de développement** — si vous souhaitez une paire de clés que vous seul possédez, supprimez `keys/private.key` et `keys/public.key` (ou définissez `License__RegenerateKeys=true` pour une exécution) avant de générer la licence que vous allez réellement installer. Chaque exécution copie également la clé publique utilisée dans le répertoire `output/` (ignoré par Git), à côté de la licence — il s'agit d'une simple copie de confort locale de la même clé publique décrite ci-dessus, et non d'une clé nouvelle ou supplémentaire, ce qui ne change donc rien à la posture de sécurité. La clé privée n'est jamais copiée hors de `keys/`.

## Installer la licence

Une fois que vous disposez de `keys/public.key` et de `output/result.gitlab-license` :

### 1. Remplacer la clé publique de chiffrement de licence de GitLab

GitLab lit sa clé publique de chiffrement de licence depuis un chemin fixe à l'intérieur de l'instance : `/opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub`. Vous devez la remplacer par la clé publique générée par cet outil, sinon la licence que vous installez à l'étape 2 sera rejetée. (`output/public.key` provenant de la même génération en est une copie identique et peut être utilisé indifféremment à la place de `keys/public.key` ci-dessous.)

**Docker / docker-compose** — montez-la en lecture seule dans votre propre `docker-compose.yml` (voir `examples/docker-compose.gitlab-ee-19.3.yml` pour un exemple complet) :

```yaml
services:
  gitlab:
    image: gitlab/gitlab-ee:19.3.0-ee.0
    volumes:
      - ./keys/public.key:/opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub:ro
      # ...your other GitLab volumes/config
```

Puis reconfigurez une fois le conteneur démarré :

```bash
docker exec gitlab gitlab-ctl reconfigure
docker exec gitlab gitlab-ctl restart
```

**Installation bare-metal / Omnibus** :

```bash
sudo cp ./keys/public.key /opt/gitlab/embedded/service/gitlab-rails/.license_encryption_key.pub
sudo gitlab-ctl reconfigure
sudo gitlab-ctl restart
```

### 2. Téléverser la licence dans GitLab

1. Connectez-vous à GitLab en tant qu'administrateur.
2. Accédez à **Admin Area > Settings > General**.
3. Repérez la section **« Add License »**, téléversez `output/result.gitlab-license`, cochez la case des conditions d'utilisation (Terms of Service), puis cliquez sur **Add License**.
4. GitLab vous redirige vers **Admin Area > Subscription**, où vous pouvez à tout moment consulter la licence installée (ainsi que, si elle inclut des add-ons, les sièges GitLab Duo qu'elle a accordés).

> Il n'existe pas de page `/admin/license/new` dans les versions actuelles de GitLab — le formulaire de téléversement se trouve sur la page des paramètres généraux ci-dessus.

### Optionnel : désactiver Service Ping

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

## Tester avec une instance GitLab locale

`examples/docker-compose.gitlab-ee-19.3.yml` est un serveur GitLab EE de référence, préconfiguré pour accepter une licence générée par cet outil (il monte déjà `../keys/public.key` au bon emplacement). Il ne fait **pas** partie du générateur — il est fourni uniquement pour vous permettre de tester la licence générée :

```bash
export GITLAB_HOME=/srv/gitlab
mkdir -p "$GITLAB_HOME/config" "$GITLAB_HOME/logs" "$GITLAB_HOME/data"
docker compose -f examples/docker-compose.gitlab-ee-19.3.yml up -d
```

## Fonctionnement

- **`GitlabLicenseGenerator.Core/Crypto`** — génération des clés RSA et enveloppe de chiffrement de la licence (GitLab chiffre son JSON de licence avec un schéma RSA/AES ; la moitié publique est fournie avec GitLab, la moitié privée reste entre les mains de celui qui émet les licences).
- **`GitlabLicenseGenerator.Core/Licensing`** — construit, valide, sérialise et exporte la licence :
  - `LicenseFeatureCatalog` — tous les symboles de fonctionnalités EE Ultimate que le `FEATURES_BY_PLAN` propre à GitLab associe au plan `ultimate`.
  - `LicenseAddOnCatalog` / `LicenseAddOnPurchase` — le mécanisme d'add-ons basés sur des sièges (`restrictions.add_on_products`), qui accorde GitLab Duo Pro, Duo Enterprise, Duo with Amazon Q, Duo Core, Duo Agent Platform Self-Hosted, GitLab Credits, Secrets Manager et Flex Offline. Cela n'est provisionné que sur une **licence cloud hors ligne** (`cloud_licensing_enabled` + `offline_cloud_licensing_enabled`, tous deux systématiquement définis par cet outil).
  - `LicenseFactory` — combine tout ce qui précède en une seule `License` disposant du maximum de permissions.
  - `LicenseValidator` / `LicenseCodec` / `LicenseJsonConverter` — validation, structure JSON compatible avec GitLab, chiffrement/export.

## Dépannage

- **HTTP 502 depuis GitLab** : attendez la fin du démarrage — cela peut prendre plusieurs minutes au premier lancement.
- **Licence rejetée** : vérifiez que vous avez bien remplacé `.license_encryption_key.pub` par le fichier `keys/public.key` *correspondant*, issu de la même génération que la licence que vous téléversez, et que vous avez redémarré GitLab après l'avoir copié.

## LICENCE

Ce projet est distribué sous licence **WTFPL License**.

Copyright (c) 2023, Tim Cook, All Rights Not Reserved.
