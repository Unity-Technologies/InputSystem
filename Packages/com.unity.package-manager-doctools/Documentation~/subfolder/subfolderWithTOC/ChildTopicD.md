# Child topic B

Another way to nest topics is to use a subfolder that contains its own `toc.yml` file that defines the TOC for that folder. To do this, link to the `toc.yml` file from the main manual TOC:

```
* [Documentation Tool](index)
* [External Links](ExternalLinks.md)
* [Parent Topic](subfolder/ParentTopicA.md)
    * [Child Topic B](subfolder/subfolder/ChildTopicB.md)
    * [Child Topic C](ChildTopicC.md)
    * [Nested TOC](subfolder/subfolderWithTOC/toc.yml)
```

In this case, the parent node in the main TOC does not have its own topic. (You can specify such a topic link when referencing from another YAML-format TOC file, but not from a markdown-format TOC file.)