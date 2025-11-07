using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace GenerateTestRequestObjects;

public static class Helpers
{
	public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(
			this IEnumerable<TSource>      source
			, Func<TSource, Task<TResult>> method)
	{
		var sourceList = source.ToList();
		var tasks = new List<TResult>(sourceList.Count);
		foreach (var item in sourceList)
			tasks.Add(await method(item));
		return tasks;
	}

	public static async Task<IEnumerable<T1>> SelectManyAsync<T, T1>(
			this IEnumerable<T>              enumeration
			, Func<T, Task<IEnumerable<T1>>> func)
	{
		return (await enumeration.SelectAsync(func)).SelectMany(x => x);
	}

	public static class SyntaxNodeHelper
	{
		public static bool TryGetParentSyntax<T>(SyntaxNode syntaxNode, out T result)
				where T : SyntaxNode
		{
			// set defaults
			result = null;

			if (syntaxNode == null)
				return false;

			try
			{
				syntaxNode = syntaxNode.Parent;

				if (syntaxNode == null)
					return false;

				if (syntaxNode.GetType() != typeof(T))
					return TryGetParentSyntax(syntaxNode, out result);

				result = syntaxNode as T;

				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
