using UnityEngine;
using System.Collections;

public struct OutflowFlux {
	public float top, bottom, left, right;
}

public struct Velocity {
	public float u, v;
}

/// <summary>
/// A layer for the fluid simulation.
/// Calculations based on http://evasion.imag.fr/Publications/2007/MDH07/FastErosion_PG07.pdf
/// </summary>
public class FluidLayer : ElementLayer {
	const int N = ElementLayerManager.N;
	const float g = 9.81f;
	
	//
	// Settings
	// -----------------------
	public float _viscosity = 0.0001f;
	public float _A = 1.0f; //the cross-sectional area of the virtual pipe
	public float _opaqueHeight = 0.5f;
	public float _nonZeroHeightOffset = 0.1f;
	
	//
	// Bindings
	// -----------------------
	Mesh _mesh;

	//
	// Simulated variables
	// -------------------------------------------
	// Primary
	float[][] _height = new float[N+2][];
	OutflowFlux[][] _flux = new OutflowFlux[N+2][];
	// Derived
	Velocity[][] _vel = new Velocity[N+2][];
	// Extra
	float[][] _sediment = new float[N+2][];
	
	//
	// Temporary values
	// -------------------------------------------
	float[][] _tempHeight = new float[N+2][];
	OutflowFlux[][] _tempFlux = new OutflowFlux[N+2][];
	
	//
	// Getters
	// --------------------------
	public override float[][] Height {
		get {
			return _height;
		}
	}

	//
	// Debug
	// --------------------
	float _totalDens;
	Timer _fluidTimer = new Timer();
	Timer _visualisationTimer = new Timer();
	
	
	
	// Use this for initialization
	void Start () {
		//
		// Init simulated variables
		// -------------------------------------------
		for (int i = 0; i < N+2; ++i) {
			_height[i] = new float[N+2];
			_flux[i] = new OutflowFlux[N+2];
			
			_vel[i] = new Velocity[N+2];
			
			_sediment[i] = new float[N+2];
			
			// temporary
			_tempHeight[i] = new float[N+2];
			_tempFlux[i] = new OutflowFlux[N+2];
		}

		// Initialize visuals
		_mesh = new Mesh();
		GetComponent<MeshFilter> ().mesh = _mesh;
		CreatePlaneMesh (10);
	}
	
	/// <summary>
	/// Add fluid to the layer based on a source array. Part 3.1 in the paper.
	/// </summary>
	/// <param name="source">The source array, this will be added directly, so it won't be multiplied with dt.</param>
	public override void AddSource(float[][] source) {
		for (int i=0 ; i<N+2 ; i++ ) {
			for (int j=0 ; j<N+2 ; j++ ) {
				_height[i][j] = Mathf.Max(0, _height[i][j] + source[i][j]);
			}
		}
	}
	/// <summary>
	/// Update the fluid. Part 3.2 in the paper.
	/// </summary>
	/// <param name="dt">delta time</param>
	/// <param name="dx">delta x. Distance between middles of grid-cells.</param>
	/// <param name="lowerLayersHeight">The total heights of the layers under this one. Basically this is the ground under the water.</param>
	public override void DoUpdate(float dt, float dx, float[][] lowerLayersHeight) {
		_fluidTimer.Start();
		
		int x, y;
		float totalHeight, dhL, dhR, dhT, dhB;
		float dt_A_g_l = dt * _A * g / dx; //all constants for equation 2
		float K; // scaling factor for the outﬂow ﬂux
		float dV;
		
		for (x=1 ; x <= N ; x++ ) {
			for (y=1 ; y <= N ; y++ ) {
				//
				// 3.2.1 Outﬂow Flux Computation
				// --------------------------------------------------------------
				totalHeight = lowerLayersHeight[x][y] + _height[x][y];
				dhL = totalHeight - lowerLayersHeight[x-1][y] - _height[x-1][y]; //(3)
				dhR = totalHeight - lowerLayersHeight[x+1][y] - _height[x+1][y];
				dhT = totalHeight - lowerLayersHeight[x][y+1] - _height[x][y+1];
				dhB = totalHeight - lowerLayersHeight[x][y-1] - _height[x][y-1];
				
				_tempFlux[x][y].left =	 Mathf.Max(0.0f, _flux[x][y].left	 + dt_A_g_l * dhL ); //(2)
				_tempFlux[x][y].right =	 Mathf.Max(0.0f, _flux[x][y].right	 + dt_A_g_l * dhR );
				_tempFlux[x][y].top =	 Mathf.Max(0.0f, _flux[x][y].top	 + dt_A_g_l * dhT );
				_tempFlux[x][y].bottom = Mathf.Max(0.0f, _flux[x][y].bottom  + dt_A_g_l * dhB );
				
				float totalFlux = _tempFlux[x][y].left + _tempFlux[x][y].right + _tempFlux[x][y].top + _tempFlux[x][y].bottom;
				if (totalFlux > 0) {
					K = Mathf.Min(1.0f, _height[x][y] * dx * dx / totalFlux / dt);  //(4)
					
					_tempFlux[x][y].left =	 K * _tempFlux[x][y].left;  //(5)
					_tempFlux[x][y].right =	 K * _tempFlux[x][y].right;
					_tempFlux[x][y].top =	 K * _tempFlux[x][y].top;
					_tempFlux[x][y].bottom = K * _tempFlux[x][y].bottom;
				}
				//swap temp and the real one after the for-loops
			}
		}
		
		for (x=1 ; x <= N ; x++ ) {
			for (y=1 ; y <= N ; y++ ) {
				//
				// 3.2.2 Water Surface and Velocity Field Update
				// ----------------------------------------------------------------------------------------
				dV = dt * (
						//sum in
						_tempFlux[x-1][y].right + _tempFlux[x][y-1].top + _tempFlux[x+1][y].left + _tempFlux[x][y+1].bottom
						//minus sum out
						- _tempFlux[x][y].right - _tempFlux[x][y].top - _tempFlux[x][y].left - _tempFlux[x][y].bottom
					); //(6)
				_tempHeight[x][y] = _height[x][y] + dV / (dx*dx); //(7)
				//swap temp and the real one after the for-loops
			}
		}
		Helpers.Swap(ref _tempFlux, ref _flux);
		Helpers.Swap(ref _tempHeight, ref _height);
		
		_fluidTimer.Stop();
		
		
		
		//Debug
		_totalDens = 0;
		for (int i=0 ; i<N+2 ; i++ ) { 
			for (int j=0 ; j<N+2 ; j++ ) {
				_totalDens += _height[i][j];
			}
		}
	}
	
	public override void ApplyVisuals(float[][] lowerLayersHeight) {
		_visualisationTimer.Start();
		
		// Set the heights of the vertices of the mesh and apply colors
		var mesh = _mesh;
		Vector3[] vertices = mesh.vertices;
		Color32[] colors = mesh.colors32;
		
		Color32 transparant = new Color32(255, 255, 255, 0);
		Color32 opaque = new Color32(255, 255, 255, 255);
		
		for (int i = 0; i < N+2; ++i) {
			for (int j = 0; j < N+2; ++j) {
				int index = CalculateIndex(i,j);
				vertices[index].y = _height[i][j] + lowerLayersHeight[i][j] + _nonZeroHeightOffset;
				colors[index] = Color32.Lerp(transparant, opaque, _height[i][j] / _opaqueHeight);
			}
		}
		
		mesh.vertices = vertices;
		mesh.colors32 = colors;
		
		_visualisationTimer.Stop();
	}
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	

	void OnGUI() {
		GUI.Label(new Rect(10,10, 500, 30), "Fluid: " + _fluidTimer);
		GUI.Label(new Rect(10, 30, 500, 30), "Visuals: " + _visualisationTimer);
		GUI.Label(new Rect(10,50, 500, 30), "Total Density: " + _totalDens);
	}

	void OnDrawGizmos () {
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
	void CreatePlaneMesh(float size) {
		var m = _mesh;
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
				vertices[index] = new Vector3(x*scaleX - size/2, 0.0f, y*scaleY - size/2);
				uvs[index++] = new Vector2(x*uvFactorX, y*uvFactorY);
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
	
	void GenerateTexture() {
		var texture = renderer.material.mainTexture as Texture2D;
		
		// set the pixel values
		for (int i = 0; i < N+2; ++i) {
			for (int j = 0; j < N+2; ++j) {
				float b = _height[i][j];
				texture.SetPixel(i, j, new Color(0, 0, b));
			}
		}
		
		// Apply all SetPixel calls
		texture.Apply();
	}
    
	public static int CalculateIndex(int i, int j) {
		return ((i) + (N + 2) * (j));
	}
}
