# External Sample Projects

These Samples are self-contained projects because they are too big to be included as a package Sample.

When editing these projects, you need to manually add a few packages for them to work.
The projects don't contain the packages themselves.

The `InputDeviceTester` needs:

- Input System package (latest version)
- Unity UI package (2.0.0 version at least)
- Jetbrains Rider Editor / Visual Studio package for IDE tooling.
- Requires the Universal Render Pipeline (URP) package to be installed for shader compilation.

The `TouchSamples` need:

- All the same as ´InputDeviceTester`
- Cinemachine (2.10.3 maximum; 3.x has errors with the project but are easy to fix)
- Probuilder (5.2.3 version maximum at the moment; 6.x has problems building for iOS and Android)
- Requires the Universal Render Pipeline (URP) package to be installed for shader compilation.
