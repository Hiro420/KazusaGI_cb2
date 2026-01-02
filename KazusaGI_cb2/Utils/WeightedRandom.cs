using System;
using System.Collections.Generic;

namespace KazusaGI_cb2.Utils;

public static class WeightedRandom
{
	private static readonly Random Rng = new();

	public static T? PickOne<T>(IReadOnlyList<(T Item, int Weight)> items)
	{
		long total = 0;
		foreach (var (_, w) in items) if (w > 0) total += w;
		if (total <= 0) return default;

		var roll = Rng.NextDouble() * total;
		long acc = 0;
		foreach (var (item, w) in items)
		{
			if (w <= 0) continue;
			acc += w;
			if (roll < acc) return item;
		}
		return default;
	}

	public static bool RollByWeight(int weight, int max = 10000) =>
		weight > 0 && Rng.Next(1, max + 1) <= weight;
}