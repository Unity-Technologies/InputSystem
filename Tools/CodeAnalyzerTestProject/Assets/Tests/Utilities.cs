using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.TestTools.TestRunner.GUI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PackageTestSuite
{
    public static class Utilities
    {
        public static string GetLogs()
        {
            var result = new StringBuilder();
            var logCount = LogEntries.GetCount();

            LogEntries.StartGettingEntries();

            for (var i = 0; i < logCount; i++)
            {
                var entry = new LogEntry();
                LogEntries.GetEntryInternal(i, entry);
                string logMessage = GetLogEntryMessage(entry);

                // Ignore any log after the test runner logs message
                if (logMessage.StartsWith("Running tests for "))
                    break;

                // Ignore log synchronization messages caused by the integration tests framework
                if (logMessage.StartsWith("Synchronize_"))
                    continue;

                // Ignore the "ReadyToStart" message logged by the framework too
                if (logMessage.StartsWith("ReadyToStart"))
                    continue;

                // Ignore the assembly reload messages. Not entirely sure why these are ending up in the Console rather than
                // only the on-disk log, but it's not something this test needs to worry about.
                if (logMessage.StartsWith("Mono: successfully reloaded assembly"))
                    continue;

                // Ignore test environment conditions
                if (logMessage.StartsWith("Rebuilding Library because the asset database could not be found!"))
                    continue;

                // Ignore bad conversion message
                // NOTE: PAI-423 will explain this exemption
                // basically creating a new project from the command line was creating this error
                // when run on Mac with Editor 2019.1.  Something about processing of the dependency packages
                if (logMessage.StartsWith("Unsupported image when converting from NSImage"))
                    continue;

				// We disable updates in the input system code analyzer project, so we 
				// don't end up analyzing other packages.
                if (logMessage.StartsWith("Project manifest update is not allowed on this project."))
                    continue;


                // Ignore render pipeline messages until they are fixed
                if (logMessage.StartsWith("Package missing for Virtual Reality SDK Oculus.") || logMessage.StartsWith("Shader warning in '") ||
                    logMessage.StartsWith("Package missing for Virtual Reality SDK OpenVR.") || logMessage.Contains("Use Metal API instead.") ||
                    logMessage.Contains("is not supported, no rendering will occur"))
                    continue;
                    
                // Ignore Validation Suite's Empty Assembly Definition test messages since the editor indicates
                // that it will not compile an assembly definition with no associated scripts
                if (logMessage.StartsWith("Assembly for Assembly Definition File") && logMessage.Contains("EmptyAsmdefAdd/AFolder/NewAsmdef.asmdef"))
                    continue;

                // When doing a project upgrade, this log can occur and it is normal (and good)
                if (logMessage.Contains("Packages were changed.")) {
                    continue;
                }

                // When running code coverage from utr
                if (logMessage.Contains("Code Coverage collection is enabled for this Unity session. Note that Code Coverage lowers Editor performance.")){
                    continue;
                }

                result.AppendFormat("{0}", logMessage);
                if (!string.IsNullOrEmpty(entry.file))
                    result.AppendFormat(" ({0}:{1})", entry.file, entry.line);
                result.AppendLine();
            }

            LogEntries.EndGettingEntries();

            return result.ToString();
        }

        static public bool HasField(this Type type, string name)
        {
            return type
                .GetFields()
                .Any(p => p.Name == name);
        }

        private static string GetLogEntryMessage(LogEntry entry) {
            // 2019.1-
            if (HasField(entry.GetType(), "condition")) {
                return (string)entry.GetType().GetField("condition").GetValue(entry);
            }

            // 2019.2+
            if (HasField(entry.GetType(), "message")) {
                return (string)entry.GetType().GetField("message").GetValue(entry);
            }

            return "";
        }
    }
}
