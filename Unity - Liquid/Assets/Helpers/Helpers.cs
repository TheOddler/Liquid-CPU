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
	
	
	
	
	
	
	
	public static void CreatePlaneMesh(Mesh m, int N, float size, Vector3 vertexOffset, Color32 defaultColor) {
		m.name = "Plane";
		
		int hCount2 = N+2;
		int vCount2 = N+2;
		int numTriangles = (N+1) * (N+1) * 6;
		int numVertices = hCount2 * vCount2;
		
		Vector3[] vertices = new Vector3[numVertices];
		Vector2[] uvs = new Vector2[numVertices];
		int[] triangles = new int[numTriangles];
		Color32[] colors = new Color32[numVertices];
		
		int index = 0;
		float uvFactorX = 1.0f/(N+1);
		float uvFactorY = 1.0f/(N+1);
		float scaleX = size/(N+1);
		float scaleY = size/(N+1);
		for (float y = 0.0f; y < vCount2; y++)
		{
			for (float x = 0.0f; x < hCount2; x++)
			{
				vertices[index] = new Vector3(x*scaleX - size/2, 0.0f, y*scaleY - size/2) + vertexOffset;
				uvs[index] = new Vector2(x*uvFactorX, y*uvFactorY);
				colors[index] = defaultColor;
				
				++index;
			}
		}
		
		index = 0;
		for (int y = 0; y < (N+1); y++)
		{
			for (int x = 0; x < (N+1); x++)
			{
				triangles[index]   = (y     * hCount2) + x;
				triangles[index+1] = ((y+1) * hCount2) + x;
				triangles[index+2] = (y     * hCount2) + x + 1;
				
				triangles[index+3] = ((y+1) * hCount2) + x;
				triangles[index+4] = ((y+1) * hCount2) + x + 1;
				triangles[index+5] = (y     * hCount2) + x + 1;
				index += 6;
			}
		}
		
		m.vertices = vertices;
		m.uv = uvs;
		m.triangles = triangles;
		m.colors32 = colors;
		m.RecalculateNormals();
	}
}
