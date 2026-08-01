#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
output_dir="${1:-$repo_root/artifacts/release}"
version="${RELEASE_VERSION:-0.0.0-local}"
publish_dir="$output_dir/publish"
artifact="$output_dir/bilitool-vn-$version-framework-dependent.tar.gz"

rm -rf "$output_dir"
mkdir -p "$publish_dir"

dotnet tool restore
dotnet publish "$repo_root/src/BiliTool.Vn.Web/BiliTool.Vn.Web.csproj" \
  --configuration Release \
  --self-contained false \
  --no-restore \
  --output "$publish_dir"

dotnet tool run dotnet-CycloneDX -- \
  "$repo_root/BiliTool.Vn.sln" \
  --output "$output_dir" \
  --filename sbom.cdx.json \
  --output-format Json \
  --exclude-test-projects \
  --disable-package-restore \
  --set-name BiliTool.Vn \
  --set-version "$version"

tar --create --gzip --file "$artifact" --directory "$publish_dir" .
sha256sum "$artifact" "$output_dir/sbom.cdx.json" > "$output_dir/SHA256SUMS"

jq -n \
  --arg version "$version" \
  --arg createdAt "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
  --arg gitCommit "$(git -C "$repo_root" rev-parse HEAD 2>/dev/null || printf unknown)" \
  --arg artifact "$(basename "$artifact")" \
  --arg artifactSha256 "$(sha256sum "$artifact" | cut -d' ' -f1)" \
  --arg sbom "sbom.cdx.json" \
  --arg sbomSha256 "$(sha256sum "$output_dir/sbom.cdx.json" | cut -d' ' -f1)" \
  '{version:$version,createdAt:$createdAt,gitCommit:$gitCommit,artifact:{file:$artifact,sha256:$artifactSha256},sbom:{file:$sbom,sha256:$sbomSha256}}' \
  > "$output_dir/release-manifest.json"

rm -rf "$publish_dir"
printf 'Release evidence created at %s\n' "$output_dir"
