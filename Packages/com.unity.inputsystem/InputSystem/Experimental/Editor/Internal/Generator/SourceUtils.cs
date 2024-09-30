using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace UnityEditor.InputSystem.Experimental.Generator
{
    internal static class SourceUtils
    {
        internal const string Header = @"// This is an auto-generated source file. Any manual edits will be lost.";
        internal const string CSharpSuffix = ".cs";
        
        private static readonly Dictionary<Type, string> typeNameCache = new();
        
        internal static string GetTypeName(Type type)
        {
            // Custom handling of primitive types that doesn't require namespace access
            if (type.IsPrimitive)
            {
                if (type == typeof(bool)) return "bool";
                if (type == typeof(int)) return "int";
                if (type == typeof(uint)) return "uint";
                if (type == typeof(float)) return "float";
                if (type == typeof(double)) return "double";
                if (type == typeof(char)) return "char";
                if (type == typeof(byte)) return "byte";
                if (type == typeof(short)) return "short";
                if (type == typeof(ushort)) return "ushort";
                if (type == typeof(nint)) return "nint";
                if (type == typeof(nuint)) return "nuint";
                if (type == typeof(string)) return "string";
            }
            else if (type.IsGenericType)
            {
                if (typeNameCache.TryGetValue(type, out string cachedString))
                    return cachedString;
                
                var tmp = new StringBuilder(type.Name.Substring(0, type.Name.IndexOf('`')));
                var args = type.GenericTypeArguments;
                tmp.Append('<');
                tmp.Append(GetTypeName(args[0]));
                for (var i = 1; i < args.Length; ++i)
                {
                    tmp.Append(", ");
                    tmp.Append(GetTypeName(args[i]));
                }
                tmp.Append('>');

                var s = tmp.ToString();
                typeNameCache.Add(type, s);
                return s;
            }
            
            return type.Name;
        }
        
        private static string GetChecksum(string path)
        {
            // TODO Consider larger buffer size if its a problem
            using FileStream stream = File.OpenRead(path);
            var sha = new SHA256Managed();
            var checksum = sha.ComputeHash(stream);
            return BitConverter.ToString(checksum);
            //return BitConverter.ToString(checksum).Replace("-", String.Empty);
        }
        
        internal static void Generate(string path, Func<string> generator)
        {
            Generate(path, generator, UnityEngine.Debug.unityLogger);
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
                    LogFileMessage(logger, LogType.Log, path, $"successfully generated. Completed in {elapsed} seconds.");
                }
                else
                {
                    stopwatch.Stop();
                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                    LogFileMessage(logger, LogType.Log, path, $"already up to date. Completed in {elapsed} seconds.");
                }
            }
            catch (Exception e)
            {
                LogFileMessage(logger, LogType.Error, path, "could not be generated due to an unexpected exception");
                logger.LogException(e);
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