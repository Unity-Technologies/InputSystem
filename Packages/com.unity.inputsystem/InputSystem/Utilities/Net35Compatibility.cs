#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA)
using System;
using System.Collections.Generic;
using System.Reflection;

// Replacements for some of .NET 4 APIs to bridge us over to when we can switch the codebase
// over to C# 6 and .NET 4 (and get rid of a ton of string.Formats and lots of new IntPtr()).

// NOTE: When switching fully back to 4, also:
//   - Get rid of all the .ToArray() expressions on string.Joins.

namespace UnityEngine.Experimental.Input.Net35Compatibility
{
    public interface IObservable<out T>
    {
        IDisposable Subscribe(IObserver<T> observer);
    }

    public interface IObserver<in T>
    {
        void OnNext(T value);
        void OnCompleted();
        void OnError(Exception exception);
    }

    public interface IReadOnlyList<T> : IEnumerable<T>
    {
        int Count { get; }
        T this[int index] { get; }
    }

    internal static class Net35Compatibility
    {
        public static T GetCustomAttribute<T>(this MemberInfo element, bool inherit)
            where T : Attribute
        {
            if (element == null)
                throw new ArgumentNullException("element");

            var attributes = element.GetCustomAttributes(typeof(T), inherit);

            var numMatches = attributes.Length;
            if (numMatches == 0)
                return null;
            if (numMatches > 1)
                throw new AmbiguousMatchException();

            return (T)attributes[0];
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo element, bool inherit)
            where T : Attribute
        {
            if (element == null)
                throw new ArgumentNullException("element");

            var attributes = element.GetCustomAttributes(typeof(T), inherit);

            foreach (var attribute in attributes)
                yield return (T)attribute;
        }
    }
}
#endif // !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0)
