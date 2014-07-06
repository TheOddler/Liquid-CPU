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
	public float _A = 1.0f; //the cross-sectional area of the virtual pipe
	public float _initialHeight = 0.1f;
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
	public override float[][] HeightField {
		get {
			return _height;
		}
	}

	//
	// Debug
	// --------------------
	float _totalVolume;
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
		
		//
		// Set initial fluid height
		// -----------------------------------------------
		for (int i=1 ; i<=N ; i++ ) {
			for (int j=1 ; j<=N ; j++ ) {
				_height[i][j] = _initialHeight;
			}
		}
		
		
		//
		// Initialize visuals
		// ----------------------------------------------
		_mesh = new Mesh();
		GetComponent<MeshFilter> ().mesh = _mesh;
		CreatePlaneMesh (10);
	}
	
	/// <summary>
	/// Add fluid to the layer based on a source array. Part 3.1 in the paper.
	/// </summary>
	/// <param name="source">The source array, this will be added directly, so it won't be multiplied with dt.</param>
	public override void AddSource(float[][] source) {
		for (int i=1 ; i<=N ; i++ ) {
			for (int j=1 ; j<=N ; j++ ) {
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
		
		OutflowFlux[][] flux = _flux;
		OutflowFlux[][] tempFlux = _tempFlux;
		float[][] height = _height;
		float[][] tempHeight = _tempHeight;
		
		int x, y;
		float localHeight = 0, totalHeight, dhL = 0, dhR = 0, dhT = 0, dhB = 0;
		float dt_A_g_l = dt * _A * g / dx; //all constants for equation 2
		float K; // scaling factor for the outﬂow ﬂux
		float dV;
		float totalFlux, h_dxdx_dt;
		float tempFluxL = 0, tempFluxR = 0, tempFluxT = 0, tempFluxB = 0;
		
		_fluidTimer.Start();
		for (x=1 ; x <= N ; x++ ) {
			for (y=1 ; y <= N ; y++ ) {
				//
				// 3.2.1 Outﬂow Flux Computation
				// --------------------------------------------------------------
				localHeight = height[x][y];
				totalHeight = lowerLayersHeight[x][y] + localHeight;
				dhL = totalHeight - lowerLayersHeight[x-1][y] - height[x-1][y]; //(3)
				dhR = totalHeight - lowerLayersHeight[x+1][y] - height[x+1][y];
				dhT = totalHeight - lowerLayersHeight[x][y+1] - height[x][y+1];
				dhB = totalHeight - lowerLayersHeight[x][y-1] - height[x][y-1];

				/*tempFluxL = Mathf.Max(0.0f, _flux[x][y].left	 + dt_A_g_l * dhL ); //(2)
				tempFluxR = Mathf.Max(0.0f, _flux[x][y].right	 + dt_A_g_l * dhR );
				tempFluxT = Mathf.Max(0.0f, _flux[x][y].top		 + dt_A_g_l * dhT );
				tempFluxB = Mathf.Max(0.0f, _flux[x][y].bottom	 + dt_A_g_l * dhB );*/
				
				tempFluxL = flux[x][y].left		 + dt_A_g_l * dhL;		tempFluxL = tempFluxL > 0 ? tempFluxL : 0; //(2)
				tempFluxR = flux[x][y].right	 + dt_A_g_l * dhR;		tempFluxR = tempFluxR > 0 ? tempFluxR : 0;
				tempFluxT = flux[x][y].top		 + dt_A_g_l * dhT;		tempFluxT = tempFluxT > 0 ? tempFluxT : 0;
				tempFluxB = flux[x][y].bottom	 + dt_A_g_l * dhB;		tempFluxB = tempFluxB > 0 ? tempFluxB : 0;

				totalFlux = tempFluxL + tempFluxR + tempFluxT + tempFluxB;
				h_dxdx_dt = localHeight * dx * dx / dt;
				if (totalFlux > h_dxdx_dt) { //if (totalFlux > 0)
					K = h_dxdx_dt / totalFlux; //; Mathf.Min(1.0f, h_dxdx_dt / totalFlux);  //(4)
					
					tempFlux[x][y].left =	 K * tempFluxL;  //(5)
					tempFlux[x][y].right =	 K * tempFluxR;
					tempFlux[x][y].top =	 K * tempFluxT;
					tempFlux[x][y].bottom =	 K * tempFluxB;
				}
				else {
					tempFlux[x][y].left =	 tempFluxL;  //(5)
					tempFlux[x][y].right =	 tempFluxR;
					tempFlux[x][y].top =	 tempFluxT;
					tempFlux[x][y].bottom = tempFluxB;
				}
				//swap temp and the real one after the for-loops
			}
		}
		_fluidTimer.Stop();
		
		for (x=1 ; x <= N ; x++ ) {
			for (y=1 ; y <= N ; y++ ) {
				//
				// 3.2.2 Water Surface and Velocity Field Update
				// ----------------------------------------------------------------------------------------
				dV = dt * (
						//sum in
						tempFlux[x-1][y].right + tempFlux[x][y-1].top + tempFlux[x+1][y].left + tempFlux[x][y+1].bottom
						//minus sum out
						- tempFlux[x][y].right - tempFlux[x][y].top - tempFlux[x][y].left - tempFlux[x][y].bottom
					); //(6)
				tempHeight[x][y] = height[x][y] + dV / (dx*dx); //(7)
				//swap temp and the real one after the for-loops
			}
		}
		
		
		Helpers.Swap(ref _tempFlux, ref _flux);
		Helpers.Swap(ref _tempHeight, ref _height);
		
		
		
		
		
		//Debug
		_totalVolume = 0;
		for (int i=0 ; i<N+2 ; i++ ) { 
			for (int j=0 ; j<N+2 ; j++ ) {
				_totalVolume += _height[i][j];
			}
		}
		_totalVolume *= dx * dx;
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
				float height = _height[i][j];
				float relHeight = height / _opaqueHeight;
				
				vertices[index].y = height + lowerLayersHeight[i][j]
					+ (height > 0 ? _nonZeroHeightOffset : 0);
				colors[index] = Color32.Lerp(transparant, opaque, relHeight);
			}
		}
		
		mesh.vertices = vertices;
		mesh.colors32 = colors;
		
		_visualisationTimer.Stop();
	}
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	

	void OnGUI() {
		GUI.Label(new Rect(10,30, 500, 30), "Fluid: " + _fluidTimer);
		GUI.Label(new Rect(10, 50, 500, 30), "Visuals: " + _visualisationTimer);
		GUI.Label(new Rect(10,70, 500, 30), "Total Volume: " + _totalVolume.ToString("0.00000000"));
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
