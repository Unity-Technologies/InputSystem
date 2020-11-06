---
uid: external-links
---

# External Links

The Doc Tools package supports uid-based markdown cross-reference syntaxes to make links to the Unity Manual, Script Reference, and dependent packages:

```
@uid
<xref:uid>
[displayed text](xref:uid)
```

Where `uid` is a unique ID that is defined as follows:

* For the Unity manual, use the page name (without the .html) — the link text is the page title by default.
* For package manual files, the uid must be assigned at the top of the markdown page you are linking to.
* For both Unity Script Reference and for package classes, use the full qualified class name — the link text is the unqualified type name.

Note that you can use these same cross-reference syntaxes within the current package and to dependent packages. These types of cross references are not supported for links to arbitrary packages.

Links directly to class members, such as methods and properties, are not currently supported for links to the Unity Script Reference, but you can use them for package class members. This should be supported in the future.

## When to use UIDs for links

Besides saving typing and clutter in your markdown text, links created this way link to a specific version of Unity or a dependent package without having to specify a version in the link text. That means when your package is updated to use a newer version of Unity or a dependent package, you don’t have to manually change the links before publishing the new docs AND the docs for the old version still link to the correct, older versions of Unity and dependent packages even though the versionless URLs for those things has moved on.

If you want a link to the latest version of the Unity docs, no matter what version of Unity that has become, continue to use a normal href-based link, such as [Unity Manual](https://docs.unity3d.com/Manual/index.html)

## Assigning a UID to a markdown page

To assign a UID to a markdown file, place the following YAML snippet as the first few lines of the file:

```
---
uid: your-uid-name
---
```

Replacing `your-uid-name` with the uid you want to use. UIDs must be unique in a project. As a convention, use dashes to separate words.

## Examples:

### Links to Unity manual:

```
* [A Unity Manual](xref:UnityManual)
* [](xref:UnityManual)
* <xref:UnityManual>
* <xref:ScriptCompilationAssemblyDefinitionFiles>
* @UnityManual
* @ScriptCompilationAssemblyDefinitionFiles
* @"UnityManual?text=Da Manual"
* @class-MeshRenderer#Lighting
* @"class-MeshRenderer?text=Lighting#Lighting"
```

* [A Unity Manual](xref:UnityManual)
* [](xref:UnityManual)
* <xref:UnityManual>
* <xref:ScriptCompilationAssemblyDefinitionFiles>
* @UnityManual
* @ScriptCompilationAssemblyDefinitionFiles
* @"UnityManual?text=Da Manual"
* @class-MeshRenderer#Lighting -- Note the link text is the Page title, not the heading title. You must use `?text=title` here:
* @"class-MeshRenderer?text=Lighting#Lighting"

### Links to the Unity Script Reference

```
* <xref:UnityEngine.MonoBehaviour>
* <xref:Unity.Jobs.JobHandle>
* @"Unity.Jobs.JobHandle?text=JobFoo"
```

* <xref:UnityEngine.MonoBehaviour>
* <xref:Unity.Jobs.JobHandle>
* @"Unity.Jobs.JobHandle?text=JobFoo"

## Caveats and limitations

A caveat to the newly supported link syntaxes is that xref:uid’s are a DocFX-specific concept. They could be difficult to migrate to Flare. So use them where they solve version-related cross-reference dilemmas, but not everywhere (at least not yet).

And finally, the package docs must be generated in the version of the Editor that matches the version of Unity you want to link to (currently 19.3, 20.1, and 20.2 are supported; everything else links to the current versionless Unity doc URL).