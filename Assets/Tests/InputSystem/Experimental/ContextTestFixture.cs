using NUnit.Framework;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental
{
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
            if (context != null)
            {
                context.Dispose();
                context = null;    
            }
        }

        protected Context context { get; private set; }
    }
}