using System.Linq;
using NUnit.Framework;
using UnityEngine.InputSystem.Editor;

internal class ViewStateCollectionTests
{
    [Test]
    [Category("AssetEditor")]
    public void CanCompareTwoEqualCollections()
    {
	    var collectionOne = new ViewStateCollection<int>(new[] { 1, 2, 3, 4 });
	    var collectionTwo = new ViewStateCollection<int>(new[] { 1, 2, 3, 4 });

		Assert.That(collectionOne, Is.EqualTo(collectionTwo));
    }

    [Test]
    [Category("AssetEditor")]
    public void CanCompareTwoUnequalCollections()
    {
	    var collectionOne = new ViewStateCollection<int>(new[] { 1, 2, 3, 4 });
	    var collectionTwo = new ViewStateCollection<int>(new[] { 2, 3, 4, 5 });

	    Assert.That(collectionOne, Is.Not.EqualTo(collectionTwo));
    }

    [Test]
    [Category("AssetEditor")]
    public void CollectionIsCachedAfterFirstEnumeration()
    {
	    var collection = new[] { 1, 2, 3, 4 };
	    var collectionOne = new ViewStateCollection<int>(collection);
	    var collectionTwo = new ViewStateCollection<int>(new[] { 1, 2, 3, 4 });

	    collectionOne.Any();
	    collection[0] = 5;

	    Assert.That(collectionOne, Is.EqualTo(collectionTwo));
    }

    [Test]
    [Category("AssetEditor")]
    public void SequenceEqualsComparesCachedCollections()
    {
	    var collection = new[] { 1, 2, 3, 4 };
	    var collectionOne = new ViewStateCollection<int>(collection);
	    var collectionTwo = new ViewStateCollection<int>(new[] { 1, 2, 3, 4 });

	    collectionOne.Any();
	    collection[0] = 5;

	    Assert.That(collectionOne.SequenceEqual(collectionTwo), Is.True);
    }
}
