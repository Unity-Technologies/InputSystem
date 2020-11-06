# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.7.0-preview] - 2020-10-29
- Return the build log string from Documentation.Generate function.

## [1.6.1-preview.2] - 2020-09-28
- Include the Linux Mono archive that was accidentally omitted from the previous release.

## [1.6.1-preview] - 2020-09-25
- Added support for package documentation generation on Linux

## [1.6.0-preview] - 2020-09-23
- Add support for separating API members on their own pages. This featrue is opt-in. Add a file named `projectMetadata.json` to the Documentation~ folder containing the JSON statement: `{"useMemberPages": true}`.
- Fixed bug that prevented classes in the global namespace from appearing in the script reference. Hint: use filter.yml to hide test classes (or put them in a namespace).
- Fixed bug in which enum values were alphabetically sorted. Now enum values are sorted by value. The value is also displayed.

## [1.5.1-preview] - 2020-09-08
- Added support for the `DOCTOOLS_DESTINATION` environment variable which overrides the `DestinationPath` variable (see [GlobalSettings](./Editor/Sources/Services/Doc/GlobalSettings.cs))

## [1.5.0-preview.2] - 2020-09-01
- Fixed version selector for localised docs (links were formatted incorrectly resulting in broken links)

## [1.5.0-preview.1] - 2020-08-25
- Added support for linking to C# keywords (like `null` and `async`) with `<see langword="keyword">` elements. Previously these were just rendered as normal text in the html output.

## [1.4.0-preview.1] - 2020-08-18
- Added support for placing markdown fragments in `Documentation~/snippets`. These fragments can be included by another markdown file, but do not become html files themselves.
 
## [1.3.0-preview.1] - 2020-07-07
- Add ScriptableAsset object to allow in-Editor batch doc generation.
- Fixed sorting of versions in the HTML version selector control
- Added minimum Unity version to HTML version selector text

## [1.2.1-preview.1] - 2020-06-18
- Fix bug introduced in 1.2.0 in which markdown files in subfolders under `Documentation~` were ignored.
- Added support for linking to `toc.yml` files in subfolders from `TableOFContents.md`.
- Added documentation about nesting content in the TOC.
- Added xrefmaps for Unity 2019.4
- Added methods, fields, and properties to the Unity xrefmaps.

### Known Issues
- Not all Unity APIs can be crossreferenced via the xrefmaps. (The mapping of Comment ID to URL in the published docs is not always predictable. Adding support for the remaining types is ongoing.)  
- Cross-references to Unity docs does not always work. A workaround is to use the **Assets > Open C# Project** menu command in the Unity Editor before generating the docs. This seems to create a working project file with the correct library references.
  
## [1.2.0-preview.1] - 2020-05-27
- Refactored doc generation code to better resolve types in external assemblies
- Some type links to other packages now work; all should list namespace if no link is available
- Files in Documentation~ are no longer moved to a different relative location before doc generation. This is a potentially breaking change if file links assumed the old file structure. Including code samples defined in a region in a C# file inside a Manual markdown file is one place such breakage could occur. To correct this, remove the lowercase `package` folder in the path. Including a code sample in the XML comment of another C# file is unaffected by this change.
-  Warnings and errors are now always logged to a text file under Logs/DocToolReports in the project folder. View the report using the **View Error Report** button. 
- Added a **Validate** option, which runs the Package Validation Suite and includes its report in the Doc Tool Report. This shows missing API docs. It also creates a dependency on the Package Validation Suite package.
- Added a **Debug Doc Build** option, which replaces the **Verbose** option. 
- Added batch generation to perform doc builds using Unity command line arguments.
  - Added support for optional `api_index.md`, which allows you to write content for the landing page of the Script Reference section.
- Added version switcher feature
- Changed header background colour to true black (#000)
- Added metadata download feature on generating documentation
- Moved breadcrumbs to main content area
- Fixed some broken CSS (a rogue hashtag)
- Added bold fonts to Roboto import for Mac users

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
