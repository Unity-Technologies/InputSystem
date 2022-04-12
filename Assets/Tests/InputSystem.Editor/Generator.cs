using System;
using System.Collections.Generic;

public class Generator<T>
{
	private readonly Func<T> m_Generate;
 
	public Generator(Func<T> generate)
	{
		m_Generate = generate ?? throw new ArgumentNullException(nameof(generate));
	}
 
	public Generator<T1> Select<T1>(Func<T, T1> f)
	{
		if (f == null)
			throw new ArgumentNullException(nameof(f));

		return new Generator<T1>(() => f(m_Generate()));
	}
 
	public T Generate()
	{
		return m_Generate();
	}

	public IEnumerable<T> Generate(int n)
	{
		for (var i = 0; i < n; i++)
		{
			yield return m_Generate();
		}
	}
}

public static class Gen
{

}