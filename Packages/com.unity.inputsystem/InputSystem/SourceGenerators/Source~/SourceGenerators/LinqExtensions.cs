using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.InputSystem.SourceGenerators
{
	public static class LinqExtensions
	{
		public static string Render<T>(this IEnumerable<T> collection, Func<T, string> renderFunc)
		{
			var sb = new StringBuilder();
			foreach (var item in collection)
			{
				sb.Append(renderFunc(item));
			}

			return sb.ToString();
		}
	}
}
