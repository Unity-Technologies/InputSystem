using NUnit.Framework;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental
{
    /// <summary>
    /// Base class fixture for tests relying on a specific
    /// <seealso cref="UnityEngine.InputSystem.Experimental.Context"/> instance.
    /// </summary>
    [Category("Experimental")]
    internal class ContextTestFixture
    {
        [SetUp]
        public void SetUp()
        {
            context = new Context();
        }

        [TearDown]
        public void TearDown()
        {
            if (context == null) 
                return;
            
            context.Dispose();
            context = null;
        }

        protected Context context { get; private set; }
    }
}