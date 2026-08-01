# Release process

The library ships as three NuGet packages, each with its own project file, all released
together under the same version number:

| Package | Project file |
| --- | --- |
| `Codecrete.SwissQRBill.Core` | [Core/Core.csproj](Core/Core.csproj) |
| `Codecrete.SwissQRBill.Generator` | [PixelCanvas/PixelCanvas.csproj](PixelCanvas/PixelCanvas.csproj) |
| `Codecrete.SwissQRBill.Windows` | [Windows/Windows.csproj](Windows/Windows.csproj) |

Each project file carries the version in three properties: `Version`, `PackageVersion`,
`FileVersion`. Between releases `Version` and `PackageVersion` carry a `-dev` prerelease
suffix (e.g. `3.5.0-dev`) so they're always ordered above the last release but never
resolved by a floating `3.*` `PackageReference` (NuGet excludes prereleases from floating
ranges by default). `FileVersion` stays a plain four-part number (e.g. `3.5.0.0`) — it
cannot hold a prerelease suffix. `AssemblyVersion` is only bumped on breaking changes and
is not touched by a release.

`README.md`, the three `*/docs/README.md` files, and the example projects'
`PackageReference` versions stay pinned to the last published release at all times —
they're only updated as part of a release, never in between. This way everyone cloning the
repository gets examples that build against a package actually available on nuget.org.

## Steps

1. Update `Core/Core.csproj`, `PixelCanvas/PixelCanvas.csproj` and `Windows/Windows.csproj`:
   set `Version` and `PackageVersion` to the release version `X.Y.Z` (drop the `-dev`
   suffix), set `FileVersion` to `X.Y.Z.0` (but leave `AssemblyVersion` alone), and update
   `PackageReleaseNotes`.
2. Update `README.md`: install command and any prose version references → `X.Y.Z`.
3. Update `Core/docs/README.md`, `PixelCanvas/docs/README.md` and `Windows/docs/README.md`
   the same way.
4. Update the example projects' `PackageReference` versions to `X.Y.Z`:
   `Examples/Basic`, `Examples/iText`, `Examples/WindowsForms`,
   `Examples/WindowsPresentationFoundation`, `Examples/WinUI`, and the `HintPath`/
   `packages.config` entries of `Examples/PDFsharp` and `Examples/MicrosoftWordAddIn`.
5. Commit as `Release vX.Y.Z` and push.
6. Run the *Publish Release to NuGet* workflow
   ([.github/workflows/release.yml](.github/workflows/release.yml)) via `workflow_dispatch`.
   It reads the version from `Core/Core.csproj`, runs the tests, packs and publishes all
   three packages, and creates and pushes the `vX.Y.Z` tag.
7. Bump the three project files to the next planned version with a `-dev` suffix (e.g.
   `3.6.0-dev`), commit as `Bump version to 3.6.0-dev for development`. Leave `README.md`,
   the `docs/README.md` files, and the example projects untouched — they keep pointing at
   `X.Y.Z` until the next release.

## Why the examples still build against HEAD

[.github/workflows/examples.yaml](.github/workflows/examples.yaml) packs the current source
into a local NuGet feed and, before building each example, overrides that example's resolved
package version for the CI run only (`dotnet add package ... --version <dev version>`, which
rewrites the checked-out `.csproj` in the runner's workspace — nothing is committed). This
means CI always validates the examples against the in-progress library code, even though the
`PackageReference` version committed to the repository stays pinned to the last release.

`Examples/PDFsharp` (legacy .NET Framework, `packages.config`) and
`Examples/MicrosoftWordAddIn` (VSTO add-in) are not covered by the workflow — they need
build tooling that isn't available on the hosted runners and have to be verified manually
before a release.
