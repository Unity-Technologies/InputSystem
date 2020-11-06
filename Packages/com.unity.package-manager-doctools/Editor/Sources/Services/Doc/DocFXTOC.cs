using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor.PackageManager.DocumentationTools.UI
{


    /// <summary>
    /// A TOC for DocFX. Currently only simple, one level TOCs are implemented.
    /// </summary>
    public class DocFXTOC : List<TOCItem>
    {
        /// <summary>
        /// Add a toc item after the item with the specified name. Currently
        /// only simple, one-level TOCs are implemented.
        /// </summary>
        /// <param name="newItem">The item to add</param>
        /// <param name="position">The zero-based position of the new item.
        /// If unspecified or negative, the new item is added to the end.</param>
        public void AddItem(TOCItem newItem, int position = -1)
        {
            if (position < 0 || position >= this.Count)
            {
                this.Add(newItem);
            }
            else
            {
                this.Insert(position, newItem);
            }
        }

        public override string ToString()
        {
            string nameSTR = "";
            string sectionHrefSTR = "";
            string topicHrefSTR = "";

            string toc = "";
            foreach (var item in this)
            {
                nameSTR = ("- name: " + item.name + "\n");
                sectionHrefSTR = ("  href: " + item.sectionHref + "\n");
                if (item.topicHref != String.Empty)
                    topicHrefSTR = ("  topicHref: " + item.topicHref + "\n");

                toc += nameSTR + sectionHrefSTR + topicHrefSTR;
            }

            return toc;
        }

        public void WriteTOC(string path)
        {
            var toc = this.ToString();
            if (toc != String.Empty)
                File.WriteAllText(path, toc);
        }

        /// <summary>
        /// Finds the index of a TOC entry given its href field.
        /// </summary>
        /// <param name="hrefSTR">The href of the item top find.</param>
        /// <returns>The zero-based index; or -1 if not found.</returns>
        public int FindIndexForEntry(string hrefSTR)
        {
            return this.Select((entry, index) => new { entry.sectionHref, index })
                .FirstOrDefault(x => x.sectionHref.Equals(hrefSTR))?.index ?? -1;
        }

        /// <summary>
        /// Adds a new TOC entry before an existing entry.
        /// </summary>
        /// <remarks>Adds the new entry to the end if the existing entry is not found.</remarks>
        /// <param name="sectionHref">The href field of the existing entry.</param>
        /// <param name="newItem">The entry to add.</param>
        public void AddBefore(string sectionHref, TOCItem newItem)
        {
            int index = FindIndexForEntry(sectionHref);
            AddItem(newItem, index);
        }

        /// <summary>
        /// Adds a new TOC entry after an existing entry.
        /// </summary>
        /// <remarks>Adds the new entry to the end if the existing entry is not found.</remarks>
        /// <param name="sectionHref">The href field of the existing entry.</param>
        /// <param name="newItem">The entry to add.</param>
        public void AddAfter(string sectionHref, TOCItem newItem)
        {
            int index = FindIndexForEntry(sectionHref);
            if (index >= 0)
                index += 1;

            AddItem(newItem, index);
        }

        public bool AddChild(string parentHref, TOCItem child)
        {
            throw new NotImplementedException("Only simple, one-level TOCs are currently supported.");
        }

    }

    public class TOCItem : IComparable
    {
        public string name = "";
        public string sectionHref = ""; // folder
        public string topicHref = ""; // topic path/file in folder


        public TOCItem(string name,
                       string sectionHref,
                       string topicHref = "")
        {
            this.name = name;
            this.sectionHref = sectionHref;
            this.topicHref = topicHref;
        }

        public int CompareTo(object obj)
        {
            TOCItem item = obj as TOCItem;
            if (item == null)
                throw new ArgumentException("obj is not a TOCItem");

            return string.Compare(sectionHref, item.sectionHref, StringComparison.CurrentCulture);
        }
    }
}

