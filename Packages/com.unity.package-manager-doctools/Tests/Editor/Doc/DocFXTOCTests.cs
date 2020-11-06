using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal class DocFXTOCTests
    {
        private DocFXTOC toc;

        [SetUp]
        public void SetupTOCForTests()
        {
            toc = new DocFXTOC();
            toc.Add(new TOCItem("first item", "dir/first", "dir/first/first.md"));
            toc.Add(new TOCItem("second item", "dir/second", "dir/second/second.md"));
            toc.Add(new TOCItem("third item", "dir/third", "dir/third/third.md"));
        }

        [Test]
        public void TestCount()
        {
            Assert.AreEqual(3, toc.Count, "Count does not match the number of items created.");
        }

        [Test]
        public void TestAddItem()
        {
            toc.AddItem(new TOCItem("Inserted second", "Dir/1.2"), 1);
            Assert.AreEqual(1, toc.FindIndexForEntry("Dir/1.2"), "Index does not match inserted order.");
            toc.AddItem(new TOCItem("Inserted first", "Dir/0.2"), 0);
            Assert.AreEqual(0, toc.FindIndexForEntry("Dir/0.2"), "Index does not match inserted first order.");
            toc.AddItem(new TOCItem("Inserted last", "Dir/n.2"));
            Assert.AreEqual(toc.Count - 1, toc.FindIndexForEntry("Dir/n.2"), "Index does not match inserted last order.");
        }

        [Test]
        public void TestAddBefore()
        {
            toc.AddBefore(toc[1].sectionHref, new TOCItem( "Inserted second", "Dir/1.2"));
            Assert.AreEqual(1, toc.FindIndexForEntry("Dir/1.2"), "Index does not match inserted order.");
            Assert.AreEqual(2, toc.FindIndexForEntry("dir/second"), "Index does not match order after insert.");
            Assert.AreEqual(3, toc.FindIndexForEntry("dir/third"), "Index does not match order after insert.");
            toc.AddBefore(toc[0].sectionHref, new TOCItem("Inserted at beginning", "Dir/o.0"));
            Assert.AreEqual(0, toc.FindIndexForEntry("Dir/o.0"), "Index for item inserted first is not 0.");
            toc.AddBefore("Doesn't exist", new TOCItem("Inserted at end", "doh"));
            Assert.AreEqual(toc.Count - 1, toc.FindIndexForEntry("doh"), "Index for item inserted before non existant item is not last.");
        }

        [Test]
        public void TestAddAfter()
        {
            toc.AddAfter(toc[0].sectionHref, new TOCItem("Inserted second", "Dir/1.2"));
            Assert.AreEqual(1, toc.FindIndexForEntry("Dir/1.2"), "Index does not match inserted order.");
            Assert.AreEqual(2, toc.FindIndexForEntry("dir/second"), "Index does not match order after insert.");
            Assert.AreEqual(3, toc.FindIndexForEntry("dir/third"), "Index does not match order after insert.");
            toc.AddAfter(toc[toc.Count-1].sectionHref, new TOCItem("Inserted at end", "Dir/o.0"));
            Assert.AreEqual(toc.Count-1, toc.FindIndexForEntry("Dir/o.0"), "Index for item inserted last is not count-1.");
            toc.AddAfter("Doesn't exist", new TOCItem("Inserted at end", "doh"));
            Assert.AreEqual(toc.Count - 1, toc.FindIndexForEntry("doh"), "Index for item inserted before non existant item is not last.");
        }

        [Test]
        public void TestFindIndex()
        {
            Assert.AreEqual(0, toc.FindIndexForEntry("dir/first"), "Index does not match creation order.");
            Assert.AreEqual(1, toc.FindIndexForEntry("dir/second"), "Index does not match creation order.");
            Assert.AreEqual(2, toc.FindIndexForEntry("dir/third"), "Index does not match creation order.");
        }

// This test is too fragile due to the insertion of dirrefent flavors of line breaks on different machines
//        [Test]
//        public void TestTOCString()
//        {
//            var expected = @"- name: first item
//  href: dir/first
//  topicHref: dir/first/first.md
//- name: second item
//  href: dir/second
//  topicHref: dir/second/second.md
//- name: third item
//  href: dir/third
//  topicHref: dir/third/third.md
//";

//            Assert.AreEqual(expected, toc.ToString()); 
//        }
    }
}
