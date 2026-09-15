FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy build infrastructure and project files first so `dotnet restore` is cached
# independently of application source changes.
COPY Directory.Build.props Directory.Packages.props GitlabLicenseGenerator.slnx ./
COPY src/GitlabLicenseGenerator.Cli/GitlabLicenseGenerator.Cli.csproj src/GitlabLicenseGenerator.Cli/
COPY src/GitlabLicenseGenerator.Core/GitlabLicenseGenerator.Core.csproj src/GitlabLicenseGenerator.Core/
RUN dotnet restore src/GitlabLicenseGenerator.Cli/GitlabLicenseGenerator.Cli.csproj

COPY src/ src/
RUN dotnet publish src/GitlabLicenseGenerator.Cli/GitlabLicenseGenerator.Cli.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /license-generator

COPY --from=build /app ./

# The repository's default keys/ pair is baked in so the image works out of the box with no setup
# (see README.md — this pair is intentionally shared across every clone/fork of this repo; replace
# it with your own if that matters for your use case, e.g. by setting License__RegenerateKeys=true
# once, or mounting your own ./keys over it). The license is written into ./output — mount it as a
# volume to persist it outside the container. ("output", not "license" — that name would collide
# with this repo's own LICENSE file on case-insensitive filesystems.) Override any License:* setting
# (see appsettings.json) via License__<Key> environment variables, e.g. -e License__ExpireYear=2600.
COPY keys/ ./keys/
RUN mkdir -p /license-generator/output
VOLUME ["/license-generator/keys", "/license-generator/output"]

# Runs as root deliberately: when ./keys or ./output is bind-mounted from the host (the documented
# way to persist them), Docker creates that host directory as root:root before the container starts,
# which a non-root container user could never write into no matter how the image itself was chowned.
# This tool makes no network calls and runs once, so the usual non-root hardening isn't worth breaking
# the "mount ./output to persist it" flow the README documents.
ENTRYPOINT ["dotnet", "glgen.dll"]
