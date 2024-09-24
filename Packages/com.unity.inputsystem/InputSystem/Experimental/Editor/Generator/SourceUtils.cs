using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    internal static class SourceUtils
    {
        internal const string Header = @"// This is an auto-generated source file. Any manual edits will be lost.";
        
        internal static void Generate(string path, Func<string> generator)
        {
            Generate(path, generator, Debug.unityLogger);
        }
        
        internal static void Generate(string path, Func<string> generator, ILogger logger)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var newContent = generator();
                var currentContent = File.Exists(path) ? File.ReadAllText(path) : null;
                if (!newContent.Equals(currentContent))
                {
                    File.WriteAllText(path, newContent);
                    
                    stopwatch.Stop();
                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                    LogFileMessage(logger, LogType.Log, path, $"successfully generated in {elapsed} seconds.");
                }
                else
                {
                    LogFileMessage(logger, LogType.Log, path, "already up to date.");
                }
            }
            catch (Exception e)
            {
                LogFileMessage(logger, LogType.Error, path, "could not be generated due to an unexpected exception");
                Debug.LogException(e);
            }
            
            stopwatch.Stop();
        }
        
        internal static string Sanitize(string name)
        {
            var open = 0;
            var convertToUpper = false;
            var sb = new StringBuilder();
            for (var i = 0; i < name.Length; ++i)
            {
                var c = name[i];
                if (char.IsWhiteSpace(c) || (!char.IsLetter(c) || char.IsNumber(c)))
                {
                    if (c == '(')
                        ++open;
                    if (c == ')')
                        --open;
                    convertToUpper = true;
                    continue;
                }

                if (open != 0)
                    continue;

                if (convertToUpper)
                {
                    sb.Append(char.ToUpper(c));
                    convertToUpper = false;
                }
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }
        
        private static void LogFileMessage(ILogger logger, LogType logType, string path, string message)
        {
            Log(logger, logType, $"\"{path}\" {message}");
        }

        private static void Log(ILogger logger, LogType logType, string message)
        {
            logger.Log(logType, $"{nameof(NodeGenerator)}: {message}.");
        }
    }
}