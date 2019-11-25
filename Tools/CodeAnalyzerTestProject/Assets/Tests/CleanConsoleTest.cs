using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TestTools;
using Is = UnityEngine.TestTools.Constraints.Is;


namespace PackageTestSuite
{
    public class CleanConsoleTest
    {
        // Make sure it is run before anything else that can affect the console
        [Test, Order(1)]
        public void TestCleanConsole()
        {
            string logs = Utilities.GetLogs();
			Assert.That(logs, Is.Empty, string.Format("Found logs in the console on project startup.\n\nLogs: {0}",logs));
        }
    }
}
