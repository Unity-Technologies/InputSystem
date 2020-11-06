using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    public class DocFXConfig
    {
        private Config config;
        private bool forceMetadataRebuild = false;

        public DocFXConfig(string json)
        {
            throw new NotImplementedException();
        }

        public DocFXConfig(PackageInfo packageInfo)
        {
            config = DocFXConfig.MakeDefaultConfig(packageInfo);
        }

        public DocFXConfig()
        {
            config = DocFXConfig.MakeDefaultConfig();

            //string json = JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        public static Config MakeDefaultConfig(PackageInfo info = null)
        {
            Config defaultConfig = new Config();

            var project = new SourceProject();
            project.src.Add(new FileMap(
                new string[] {  },
                new string[] { "**/obj/**", "**/bin/**", "_site/**" }));
            project.dest = "api";
            project.filter = "filter.yml";
            project.force = true;
            defaultConfig.metadata.Add(project);

            defaultConfig.build.content.Add(
                new FileMap( new string[] { "api/**.yml", "api/index.md" }));
            defaultConfig.build.content.Add(
                new FileMap(new string[] {
                    "manual/**.md",
                    "manual/**/toc.yml"
                }));
            defaultConfig.build.content.Add(
                new FileMap(new string[] {
                    "changelog/**.md",
                    "changelog/**/toc.yml"
                }));
            defaultConfig.build.content.Add(
                new FileMap(new string[] {
                    "license/**.md",
                    "license/**/toc.yml"
                }));
            defaultConfig.build.content.Add( new FileMap(
                new string[] {
                    "toc.yml",
                    "*.md"
                },
                new string[] { //exclude
                    "obj/**",
                    "_site/**"
                }
            ));

            defaultConfig.build.resource.Add(new FileMap(
                new string[] {
                    "images/**",
                    "logo.svg"
                },
                new string[] { //exclude
                    "obj/**",
                    "_site/**"
                }
            ));
            defaultConfig.build.overwrite.Add(new FileMap(
                 new string[] {"apidoc/**.md"},
                 new string[] { //exclude
                    "obj/**",
                    "_site/**"
                            }
            ));
            defaultConfig.build.template.Add("./_exported_templates/packages/");
            defaultConfig.build.noLangKeyword = false;
            defaultConfig.build.keepFileLink = false;
            defaultConfig.build.dest = "_site";
            defaultConfig.build.xrefService.Add("https://xref.docs.microsoft.com/query?uid={uid}");
            defaultConfig.build.globalMetadata.Add("_enableSearch", true);
            defaultConfig.build.globalMetadata.Add("_appLogoPath", "logo.svg");
            defaultConfig.build.globalMetadata.Add("_disableToc", false);
            defaultConfig.build.globalMetadata.Add("enableTocForManual", false);
            defaultConfig.build.globalMetadata.Add("_generatedOn", DateTime.Today.ToString("D", CultureInfo.CurrentCulture));

            if (info != null)
            {
                defaultConfig.build.globalMetadata.Add("_appTitle", info.displayName);
                defaultConfig.build.globalMetadata.Add("_packageName", info.name);
                defaultConfig.build.globalMetadata.Add("_packageVersion", info.version);
            }
            else
            {
                defaultConfig.build.globalMetadata.Add("_appTitle", "Unknown");
                defaultConfig.build.globalMetadata.Add("_packageName", "Unknown");
                defaultConfig.build.globalMetadata.Add("_packageVersion", "Unknown");
            }
            //set default values
            return defaultConfig;
        }

        public string WriteConfig(string path)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json);
            return path;
        }

        public void SetGlobalMetadata(string key, object value)
        {
            if (config.build.globalMetadata.ContainsKey(key))
            {
                config.build.globalMetadata[key] = value;
            }
            else
            {
                config.build.globalMetadata.Add(key, value);
            }
        }
        
        public Dictionary<string, object> GetGlobalMetadata()
        {
            return config.build.globalMetadata;
        }

        public void SetTOCEnabled(bool state)
        {
            SetGlobalMetadata("enableTocForManual", state);
        }

        public void SetForceRebuildOption(bool state)
        {
            forceMetadataRebuild = state;
            foreach (var metadata in config.metadata)
            {
                metadata.force = state;
            }
        }

        public void SetGlobalNamespace(string name)
        {
            foreach (var metadata in config.metadata)
            {
                metadata.globalNamespaceID = name;
            }
        }

        public void SetNoLangKeywordOption(bool state)
        {
            config.build.noLangKeyword = state;
        }

        public void AddCSProject(string projectFilePath, string rootFolder = "")
        {
            config.metadata[0].src[0].files.Add(projectFilePath);
            if (rootFolder != String.Empty)
                config.metadata[0].src[0].src = rootFolder;
        }
        public void AddCSProjectProperties(string key, string value)
        {
            config.metadata[0].properties.Add(key, value);
        }

        public void AddContentToSection(string path, string section, string[] filePatterns)
        {
            var fileSet = new FileMap(path, section, filePatterns, new string[] { });
            config.build.content.Add(fileSet);
        }
        public void AddContentToSection(string path, string section, string[] filePatterns, string[] excludePatterns)
        {
            var fileSet = new FileMap(path, section, filePatterns, excludePatterns);
            config.build.content.Add(fileSet);
        }
        public void AddImageFolder(string path, string destinationFolder, string[] filePatterns)
        {
            var fileSet = new FileMap(path, destinationFolder, filePatterns, new string[] { });
            config.build.resource.Add(fileSet);
        }
        public void AddXrefmapURL(string url)
        {
            config.build.xref.Add(url);
        }
        public void AddDllReferences(string dllFilePath, string rootFolder)
        {
            var reference = new FileMap(rootFolder, new string[]{dllFilePath}, new string[] { });
            config.metadata[0].references.Add(reference);
        }
        public void AddDocFXTemplate(string templateContentFolder)
        {
            config.build.template.Add(templateContentFolder);
        }

        /*
         * Metadata:
         * AddProject(path)
         * SetOutputFolder(path)
         * UseFullBuild(bool)
         * ParseMarkdownInAPI(bool)
         * SetMSBuildProperty(name,value)
         *
         * Build:
         * AddConceptualFolder(path)
         * AddResourceFolder(path)
         * AddOverwriteFolder(path)
         * AddGlobalMetadata(name,value)
         * AddFileMetadataFiles(string[] paths) prop
         * Get/SetTemplatePaths(string[]) prop
         * Get/SetThemePaths(string[]) prop
         * AddXrefURL(string)
         * ExportRawModel(string path)
         * ExportViewModel(string path)
         * DryRun(bool) prop
         * MaxParallelism{get/set} prop
         * MarkdownEngine string prop
         * MarkdownEngineProperties json prop
         * NoLangKeyword bool prop
         * KeepFileLink bool prop
         * Sitemap SiteMapOptions prop
         */
    }

    public class Config
    {
        public List<SourceProject> metadata;
        public BuildConfig build;

        public Config()
        {
            metadata = new List<SourceProject>(4);
            build = new BuildConfig();
        }
    }

    public class BuildConfig
    {
        public List<FileMap> content;
        public List<FileMap> resource;
        public List<FileMap> overwrite;
        public bool noLangKeyword;
        public bool keepFileLink;
        public bool cleanupCacheHistory;
        public bool disableGitFeatures;
        public bool exportViewModel;
        public bool exportRawModel;
        public bool dryRun;
        public int maxParallelism;
        public string dest;
        public string markdownEngineName;
        public string markdownEngineProperties;
        public string viewModelOutputFolder;
        public string rawModelOutputFolder;
        public Dictionary<string, object> globalMetadata;
        //public Dictionary<string, string> fileMetadata; // the format for these is unclear
        public List<string> globalMetadataFiles;
        public List<string> fileMetadataFiles;
        public List<string> template;
        public List<string> postprocessors;
        public List<string> theme;
        public List<string> xref;
        public List<string> xrefService;
        public SiteMapOptions sitemap;

        public BuildConfig()
        {
            content = new List<FileMap>(4);
            resource = new List<FileMap>(4);
            overwrite = new List<FileMap>(4);
            globalMetadata = new Dictionary<string, object>();
            globalMetadataFiles = new List<string>(4);
            //fileMetadataFiles = new Dictionary<string, string>();
            template = new List<string>(4);
            postprocessors = new List<string>(4);
            theme = new List<string>(4);
            xref = new List<string>(4);
            xrefService = new List<string>(4);
            sitemap = new SiteMapOptions();
        }
    }

    public class SourceProject
    {
        public List<FileMap> src;
        public List<FileMap> references;
        public string dest;
        public string version;
        public string filter;
        public bool disableGitFeatures;
        public bool shouldSkipMarkup;
        public bool useCompatibilityFileName;
        public bool force;
        public Dictionary<string, string> properties;
        public string globalNamespaceID;

        public SourceProject()
        {
            src = new List<FileMap>(4);
            references = new List<FileMap>(1);
            properties = new Dictionary<string, string>();
            globalNamespaceID = "";
        }
    }

    public class FileMap
    {
        public List<string> files;
        public List<string> exclude;
        public string src;
        public string dest;

        public FileMap()
        {
            files = new List<string>(4);
            exclude = new List<string>(4);
        }
        public FileMap(string src, string dest, string[] files, string[] exclude)
        {
            this.src = src;
            this.dest = dest;
            this.files = new List<string>(files.Length);
            this.files.AddRange(files);
            this.exclude = new List<string>(exclude.Length);
            this.exclude.AddRange(exclude);
        }
        public FileMap(string src, string[] files, string[] exclude)
        {
            this.src = src;
            this.files = new List<string>(files.Length);
            this.files.AddRange(files);
            this.exclude = new List<string>(exclude.Length);
            this.exclude.AddRange(exclude);
        }
        public FileMap(string[] files, string[] exclude)
        {
            this.files = new List<string>(files.Length);
            this.files.AddRange(files);
            this.exclude = new List<string>(exclude.Length);
            this.exclude.AddRange(exclude);
        }
        public FileMap(string[] files)
        {
            this.files = new List<string>(files.Length);
            this.files.AddRange(files);
            this.exclude = new List<string>(4);
        }
        public FileMap(string file)
        {
            this.files = new List<string>(1);
            this.files.Add(file);
            this.exclude = new List<string>(4);
        }
    }

    public class SiteMapOptions
    {
        public string baseURL;
        public DateTime lastmod;
        public string changefreq;
        public double priority;
        public Dictionary<string, SiteMapOptions> fileOptions;

        public SiteMapOptions()
        {
            fileOptions = new Dictionary<string, SiteMapOptions>();
        }
    }
}
