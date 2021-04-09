### Description

_Please fill this section with a description what the pull request is trying to address._

### Changes made

_Please write down a short description of what changes were made._

### Notes

_Please write down any additional notes, remove the section if not applicable._

### Checklist

Before review:

- [ ] Changelog entry added.
    - Explains the change in `Changed`, `Fixed`, `Added` sections.
    - For API change contains an example snippet and/or migration example.
    - FogBugz ticket attached, example `([case %number%](https://issuetracker.unity3d.com/issues/...))`.
    - FogBugz is marked as "Resolved" with *next* release version correctly set.
- [ ] Tests added/changed, if applicable.
    - Functional tests `Area_CanDoX`, `Area_CanDoX_EvenIfYIsTheCase`, `Area_WhenIDoX_AndYHappens_ThisIsTheResult`.
    - Performance tests.
    - Integration tests.
- [ ] Docs for new/changed API's.
    - Xmldoc cross references are set correctly.
    - Added explanation how the API works.
    - Usage code examples added.
    - The manual is updated, if needed.

During merge:

- [ ] Commit message for squash-merge is prefixed with one of the list:
    - `NEW: ___`.
    - `FIX: ___`.
    - `DOCS: ___`.
    - `CHANGE: ___`.
    - `RELEASE: 1.1.0-preview.3`.
