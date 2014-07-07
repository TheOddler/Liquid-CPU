using UnityEngine;
using System;
using System.Collections;

public static class Helpers {

	public static void Swap<T>(ref T first, ref T second) {
		T temp = first;
		first = second;
		second = temp;
	}

	public static void CopyValuesFrom<T>(this T[][] destination, T[][] source) {
		int x, y;
		for (x = 0; x < destination.Length; ++x) {
			for (y = 0; y < destination[x].Length; ++y) {
				destination[x][y] = source[x][y];
			}
		}
	}
	
	public static float[][] CopyArray(float[][] source)
	{
		var len = source.Length;
		var dest = new float[len][];
		
		for (var x = 0; x < len; x++)
		{
			var inner = source[x];
			var ilen = inner.Length;
			var newer = new float[ilen];
			Array.Copy(inner, newer, ilen);
			dest[x] = newer;
		}
		
		return dest;
	}
	
}
