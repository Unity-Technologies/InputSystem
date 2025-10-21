### Description

_Please fill this section with a description what the pull request is trying to address and what changes were made._


### Testing status & QA

_Please describe the testing already done by you and what testing you request/recommend QA to execute. If you used or created any testing project please link them here too for QA._

### Overall Product Risks

_Please rate the potential complexity and halo effect from low to high for the reviewers. Note down potential risks to specific Editor branches if any._

- Complexity: 
- Halo Effect: 

### Comments to reviewers

_Please describe any additional information such as what to focus on, or historical info for the reviewers._

### Checklist

Before review:

- [ ] Changelog entry added.
    - Explains the change in `Changed`, `Fixed`, `Added` sections.
    - For API change contains an example snippet and/or migration example.
    - JIRA ticket linked, example ([case %<ID>%](https://issuetracker.unity3d.com/product/unity/issues/guid/<ID>)). If it is a private issue, just add the case ID without a link.
    - Jira port for the next release set as "Resolved".
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

After merge:

- [ ] Create forward/backward port if needed. If you are blocked from creating a forward port now please add a task to ISX-1444.
