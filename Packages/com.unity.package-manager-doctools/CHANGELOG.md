# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.1-preview.5] - 2019-09-23
- Updated the Google Tag Manager code
- Changed default `_apptitle` setting to `PACKAGE DISPLAY NAME | VERSION`
- Added persistant table of contents filtering

## [1.1.1-preview.4] - 2019-08-29
- Enable hyperlinks to the .NET System class documentation in script reference using the Microsoft [Cross reference service](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#cross-reference-services).
 - Add per-project metadata support
 - Changed default `_apptitle` setting to "Unity Documentation"

## [1.1.1-preview.3] - 2019-08-22
- Updated default `filter.yml` to not exclude `ObsoleteAttribute`

## [1.1.1-preview.2] - 2019-08-13
- Added custom `filter.yml` documentation
- Updated link colors to match Unity main style

## [1.1.1-preview.1] - 2019-06-19
- Adds preprocessor directives capability
- Fixed `System.InvalidOperationException: Sequence contains no elements` error when generating without a manual

## [1.1.0-preview.1] - 2019-05-13
- Upgrade doctools for newer visual element styles
- Fixed generate button taking no height

## [1.0.0-preview.35] - 2019-05-02
- Fixed filter.yml issue on windows

## [1.0.0-preview.34] - 2019-05-01
- Stopped building tiny runtime docs

## [1.0.0-preview.33] - 2019-04-15
- Updated copyright years

## [1.0.0-preview.32] - 2019-03-26
- Updated ads redirect for v3.0

## [1.0.0-preview.31] - 2019-03-21
- Fixed mono bin folder on mac

## [1.0.0-preview.30] - 2019-03-18
- Fixed verbose logging on windows

## [1.0.0-preview.29] - 2019-03-16
- Fixed mono on mac

## [1.0.0-preview.28] - 2019-03-16
- Adds serve feedback in verbose mode

## [1.0.0-preview.27] - 2019-03-16
- Fixes mono usage

## [1.0.0-preview.26] - 2019-03-13
- Fixed doc generation with C# 7.2 features like in
- Removed documentation with members with the `Obsolete` attribute
- Fixed links across namespaces
- Added ability to add an overriding filter.yml inside the `Documentation~` folder
- Adds verbose mode for debugging when in developer mode

## [1.0.0-preview.25] - 2019-02-20
- Fixed header breaking correctly per section

## [1.0.0-preview.24] - 2019-02-20
- Fixed header breaking at any letter

## [1.0.0-preview.23] - 2019-12-02
- Fixed version conversion to support different CultureInfos
- Removed cluttering in pages by filtering out System. inherited members

## [1.0.0-preview.22] - 2019-01-01
- Update redirection rules for package online documentations
- Fix @latest doc generation for older packages

## [1.0.0-preview.21] - 2018-12-11
- Changed runtime folder name to rt for Tiny

## [1.0.0-preview.20] - 2018-11-26
- Fixes Runtime API section generation for Tiny package
- Adds ability to use ?preview=1 to @latest page to link to latest preview version

## [1.0.0-preview.19] - 2018-11-22
- Create a Runtime API section for Tiny package

## [1.0.0-preview.18] - 2018-11-06
- Fixed button text

## [1.0.0-preview.17] - 2018-11-01
- Support UIElement out of experimental

## [1.0.0-preview.16] - 2018-10-31
- Show doctools on built in packages

## [1.0.0-preview.15] - 2018-10-23
- Fixed blockquote font size

## [1.0.0-preview.14] - 2018-10-19
- Fixed standardevents redirect url

## [1.0.0-preview.13] - 2018-10-19
- Modified analytics behavior on later versions

## [1.0.0-preview.12] - 2018-08-20
- Fixed indent on table of content

## [1.0.0-preview.11] - 2018-08-17
- Fixed site generation while not in developer mode

## [1.0.0-preview.10] - 2018-05-17
- Fixed table of content with large decreasing indents

## [1.0.0-preview.8] - 2018-05-17
- Fixed null pointer error when installing this package while Package Manager UI is open

## [1.0.0-preview.7] - 2018-04-19
- Added documentation
- Added ability to have a table of content for user manual
- Added a license section

## [0.5.2] - 2018-04-10
- Removed internal menu

## [0.5.1] - 2018-04-03
- Fixed readonly error on windows when generating documentation

## [0.5.0] - 2018-02-13
- Add Generate Documentation to Package Manager UI.

## [0.3.0] - 2018-02-09
- Adds the public void GenerateRedirect(string packageName, string latestVersionId, string outputFolder = null) API to allow building the package@latest redirect site.

## [0.1.0] - 2018-02-09
### This is the first release of *Unity Package Manager Doctools*.
