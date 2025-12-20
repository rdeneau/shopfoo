# GitHub Actions Workflows

This project uses two separate GitHub Actions workflows for different use cases.

## 1. Build and Test (`build.yml`)

**Automatic trigger** on:

- Push to `main` and `develop` branches
- Pull requests to `main` and `develop`

**Purpose**: Verify code compilation and quality

**Actions**:

- ✅ Build .NET solution
- ✅ Install Node/Yarn dependencies
- ⚠️ Unit tests (to be added when test projects are created)

## 2. Release and Deploy to Azure (`CI.yml`)

**Manual trigger** from GitHub Actions

**Purpose**: Create a release and deploy to Azure

**Actions**:

1. **Semantic Release**:
   - Analyzes conventional commits since the last release
   - Determines the next version (major/minor/patch)
   - Updates `version` and `releaseDate` in `package.json`
   - Generates the `CHANGELOG.md`
   - Creates a Git tag and a GitHub Release
   - Commits the changes with `[skip ci]`

2. **Build** (only if a new release was created):
   - Retrieves the code with the updated `package.json`
   - Builds and publishes the application

3. **Deploy** (only for workflow_dispatch):
   - Deploys to Azure Web App

### Options

- **dry-run**: `true` | `false` (default: `false`)
  - Test mode that simulates the release without creating actual tags/releases

## Semantic Release Configuration

The configuration is located in `.releaserc.json`.

### Versioning Rules (Conventional Commits)

- `feat:` → **minor** version (1.x.0)
- `fix:`, `perf:`, `refactor:` → **patch** version (1.0.x)
- `BREAKING CHANGE:` → **major** version (x.0.0)
- `docs:`, `chore:`, `style:`, `test:` → no release

### Commit Examples

```bash
# Patch release (1.0.0 → 1.0.1)
git commit -m "fix: correct login bug"

# Minor release (1.0.0 → 1.1.0)
git commit -m "feat: add favorites management"

# Major release (1.0.0 → 2.0.0)
git commit -m "feat!: complete API redesign

BREAKING CHANGE: /api/v1/* endpoints are removed"
```

💡 Tip: Use [Commitji](https://github.com/rdeneau/commitji).

## Release Date Update Script

The `update-release-date.js` script is automatically executed by semantic-release to update the `releaseDate` field in `package.json` with the current date in YYYY-MM-DD format.
