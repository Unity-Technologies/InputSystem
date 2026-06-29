# CI Changes

This file documents notable changes made to the CI cookbook (`Tools/CI/`), including the motivation, what was changed, and any follow-up actions required.

---

## Wrench 2.12.0 Migration & CI Fixes
**Branch:** `bugfix/fix-ci-failure-due-to-warnings`  
**Date:** June 2026  
**Author:** Darren Kelly

### Summary
Migrated the CI cookbook from `RecipeEngine.Modules.Wrench` 2.2.1 to 2.12.0 and fixed several CI failures that surfaced as a result.

---

### 1. Wrench Version Bump
**File:** `InputSystem.Cookbook.csproj`

Bumped `RecipeEngine.Modules.Wrench` from `2.2.1` to `2.12.0`. The version in `wrench_config.json` is updated automatically by the regenerate script.

---

### 2. BaseRecipe — Migrated to Wrench 2.x Job Iteration Pattern
**File:** `Recipes/BaseRecipe.cs`

`GetJobs()` was still using the Wrench 1.x pattern, reading platforms only from `UnityEditors[0]` and iterating `SupportedEditorVersions` as a flat list. This meant per-editor platform customisations were silently ignored for all editor versions except the first.

Migrated to the 2.x pattern: iterates `package.UnityEditors` directly, using `unityEditor.Version.Version` for the version string and `unityEditor.EditorPlatforms.Items` for the platform set. Job names now use `EditorPlatformType` (e.g. `MacOs13`, `MacOs13Arm`) instead of `SystemType` (e.g. `MacOS`) to produce unique names — necessary because Wrench 2.12.0 adds `MacOs13Arm` by default and both `MacOs13` and `MacOs13Arm` share `SystemType.MacOS`.

---

### 3. Mac ARM64 Fix for Unity 6.6
**File:** `Settings/InputSystemSettings.cs`

`OverridePackagePlatform` was unconditionally forcing `MacOs13` (Intel) onto all editor versions including Unity 6.6. Wrench 2.12.0 defaults Unity 6.6 to `MacOs13Arm` (Apple Silicon), so this override was reverting 6.6 back to an Intel agent and causing `Bad CPU type in executable` failures on standalone tests.

Fixed by skipping the `MacOs13` override for Unity 6.6, allowing Wrench's default `MacOs13Arm` agent to take effect.

**Jobs fixed:**
- `StandaloneFunctionalTests - 6000.6 - MacOs13`
- `StandaloneIl2CppFunctionalTests - 6000.6 - MacOs13`
- All other custom recipe jobs for Unity 6.6 on Mac

---

### 4. Ubuntu Standalone Tests — Switched to GPU VM
**File:** `Settings/InputSystemSettings.cs`

The default Ubuntu2204 agent is a CPU-only VM (`Unity::VM`), which uses software rendering. Standalone player tests run graphical processes that are 2–3× slower under software rendering, causing frequent timeouts and flaky failures.

Overrode the Ubuntu2204 agent to use `ResourceType.VmGpu` (`Unity::VM::GPU`) with `package-ci/ubuntu-22.04:v4`. This is consistent with how other Unity teams (Burst, HDRP, UX Engineering) run Linux standalone tests. The GPU VM provisions an NVIDIA RTX 2080 Ti by default.

**Jobs fixed:**
- `StandaloneFunctionalTests - 6000.0 - Ubuntu2204`
- `StandaloneFunctionalTests - 6000.3 - Ubuntu2204`
- All other standalone recipe jobs on Ubuntu2204

---

### Known Outstanding Issues

- **`parentSpanId` deserialization error** — Unity 6.6 editor builds (from `6000.6.0a8` onwards) include a test framework change (PR #102587 in `unity/unity`) that emits `parentSpanId: ""` instead of `null`, which UTR cannot deserialize. This is a known upstream issue being tracked in PR #109932 in `unity/unity`. No action required on our side; monitor that PR for a fix.

- **`Ubuntu2004 packAndPromotePlatformType` warning** — Wrench logs a warning that `Ubuntu2004` is set as the `packAndPromotePlatformType` but is not in the editor platforms. This is pre-existing and non-blocking (generation still succeeds). Investigate whether `packAndPromotePlatformType` should be updated to `Ubuntu2204`.
