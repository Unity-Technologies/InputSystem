# Contributions

## If you are interested in contributing, here are some ground rules:
* Ideally, [talk to us](https://forum.unity.com/forums/input-system.103/) before doing the work -- we appreciate contributions, but we might already be working on the same thing, or we might have different opinions on how it should be implemented.

## All contributions are subject to the [Unity Contribution Agreement(UCA)](https://unity3d.com/legal/licenses/Unity_Contribution_Agreement)
By making a pull request, you are confirming agreement to the terms and conditions of the UCA, including that your Contributions are your original creation and that you have complete right and authority to make your Contributions.

## Once you have a change ready that complies to the rules above, open a pull request in GitHub.

Notes:

* We can rarely take a pull request directly and as-is because our CI doesn't run on PRs coming from forks. So, in most cases we will have to incorporate your changes into a branch inside the repository and merge it into the `develop` branch via a separate PR. When we do so, we will clearly state that in your PR.
* All changes are subject to the following requirements:
  1) Tests need to be green.
  2) Behavioral changes need to be accompanied by test changes/additions.
  3) Where relevant, documentation (xmldoc and/or Manual) needs to be updated.
  4) Fixes, changes, and additions need to be documented in [CHANGELOG](./CHANGELOG.md).
  5) Breaking API changes can only be introduced in new major versions.
* The more a PR deviates from these requirements, the more work we have to do on our side to incorporate your changes. Give your PR a leg up by making it easy on us :)
* We are hoping to have an easier workflow for incorporating user PRs in the future.
