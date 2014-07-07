using UnityEngine;
using System.Collections;
using System.Threading;

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
	// Visuals
	// -----------------------
	Mesh _mesh;
	Vector3[] _vertices;
	Color32[] _colors;

	//
	// Simulated variables
	// -------------------------------------------
	// Primary
	float[][] _height = new float[N+2][];
	OutflowFlux[][] _flux = new OutflowFlux[N+2][];
	float[][] _source = new float[N+2][];
	// Derived
	Velocity[][] _velocity = new Velocity[N+2][];
	// Extra
	float[][] _sediment = new float[N+2][];
	
	//
	// Temporary values
	// -------------------------------------------
	float[][] _tempLowerLayersHeight = new float[N+2][];
	float[][] _tempHeight = new float[N+2][];
	OutflowFlux[][] _tempFlux = new OutflowFlux[N+2][];
	float[][] _tempSource = new float[N+2][];
	
	Velocity[][] _tempVelocity = new Velocity[N+2][];

	//
	// Threading
	// ------------------------------------
	Thread _updateThread;

	//
	// Getters
	// --------------------------
	public override float[][] HeightField {
		get {
			return _height;
		}
	}
	public Velocity[][] VelocityField {
		get {
			return _velocity;
		}
	}

	//
	// Debug
	// --------------------
	float _totalVolume;
	Timer _waitTimer = new Timer();
	Timer _applyUpdateTimer = new Timer();
	Timer _updateTimer = new Timer();
	
	void Awake() {
		//
		// Init simulated variables
		// -------------------------------------------
		for (int i = 0; i < N+2; ++i) {
			_height[i] = new float[N+2];
			_flux[i] = new OutflowFlux[N+2];
			_source[i] = new float[N+2];
			
			_velocity[i] = new Velocity[N+2];
			
			_sediment[i] = new float[N+2];
			
			// temporary
			_tempLowerLayersHeight[i] = new float[N+2];
			_tempHeight[i] = new float[N+2];
			_tempSource[i] = new float[N+2];
			_tempFlux[i] = new OutflowFlux[N+2];

			_tempVelocity[i] = new Velocity[N+2];
		}
		
		//
		// Initialize visuals
		// ----------------------------------------------
		_mesh = new Mesh();
		GetComponent<MeshFilter> ().mesh = _mesh;
		CreatePlaneMesh (10);
		_vertices = _mesh.vertices;
		_colors = _mesh.colors32;
	}
	
	/// <summary>
	/// Add fluid to the layer based on a source array. Part 3.1 in the paper.
	/// </summary>
	/// <param name="source">The source array, this will be added directly, so it won't be multiplied with dt.</param>
	public override void AddSource(float[][] source) {
		for (int i=1 ; i<=N ; i++ ) {
			for (int j=1 ; j<=N ; j++ ) {
				_source[i][j] += source[i][j];
			}
		}
	}
	
	public override void Initialize(float dt, float dx, float[][] lowerLayersHeight) {
		//
		// Set initial fluid height
		// -----------------------------------------------
		for (int i=1 ; i<=N ; i++ ) {
			for (int j=1 ; j<=N ; j++ ) {
				_height[i][j] = _tempHeight[i][j] = _initialHeight;
			}
		}
		
		//
		// Already start the first update so one is ready when asked for in the DoUpdate
		// ----------------------------------------------------------------------------------------------
		DoUpdateThreaded(dt, dx, lowerLayersHeight);
	}
	/// <summary>
	/// Update the fluid. Part 3.2 in the paper.
	/// </summary>
	/// <param name="dt">delta time</param>
	/// <param name="dx">delta x. Distance between middles of grid-cells.</param>
	/// <param name="lowerLayersHeight">The total heights of the layers under this one. Basically this is the ground under the water.</param>
	public override void DoUpdate(float dt, float dx, float[][] lowerLayersHeight) {
		
		_applyUpdateTimer.Start();
		
		_waitTimer.Start();
		//Wait if the update isn't ready yet
		if (_updateThread.IsAlive) {
			_updateThread.Join();
		}
		_waitTimer.Stop();
		
		//Apply all updated values
		Helpers.Swap(ref _tempFlux, ref _flux);
		Helpers.Swap(ref _tempHeight, ref _height);
		Helpers.Swap(ref _tempSource, ref _source);
		Helpers.Swap(ref _tempVelocity, ref _velocity);
		_mesh.vertices = _vertices;
		_mesh.colors32 = _colors;
		//Start a new update for the next time DoUpdate is called.
		DoUpdateThreaded(dt, dx, lowerLayersHeight);
		
		_applyUpdateTimer.Stop();

		
		
		//Debug
		_totalVolume = 0;
		for (int i=0 ; i<N+2 ; i++ ) { 
			for (int j=0 ; j<N+2 ; j++ ) {
				_totalVolume += _height[i][j];
			}
		}
		_totalVolume *= dx * dx;
	}
	
	
	void DoUpdateThreaded(float dt, float dx, float[][] lowerLayersHeight) {
		
		//Create the buffer for the lowerLayersHeight in case it'll get altered on the main thread while the fluid simulation thread is running.
		//This is very likely and was the reason of some very weird bugs
		_tempLowerLayersHeight.CopyValuesFrom(lowerLayersHeight);
		
		//Create and start the simulation thread.
		_updateThread = new Thread(()=>{
			_updateTimer.Start();
			
			DoAddSourceToHeight(_tempSource, _height);
			UpdateFlux(dt, dx, _tempLowerLayersHeight, _A, _flux, _tempFlux, _height);
			UpdateHeight(dt, dx, _tempLowerLayersHeight, _tempFlux, _height, _tempHeight);
			UpdateVelocity(dx, _tempFlux, _height, _tempHeight, _tempVelocity);
			
			UpdateVisuals(_tempLowerLayersHeight, _opaqueHeight, _nonZeroHeightOffset, _height, _vertices, _colors);
			
			_updateTimer.Stop();
		});
		_updateThread.Start();
	}
	
	static void DoAddSourceToHeight(float[][] source, float[][] height) {
		int x, y;
		float newHeight;
		for (x=1 ; x <= N ; x++ ) {
			for (y=1 ; y <= N ; y++ ) {
				newHeight = height[x][y] + source[x][y];
				height[x][y] = newHeight > 0 ? newHeight : 0;
				
				source[x][y] = 0.0f;
			}
		}
	}
	
	static void UpdateFlux(float dt, float dx, float[][] lowerLayersHeight,
		float A,
		OutflowFlux[][] flux, OutflowFlux[][] tempFlux,
		float[][] height
		) {
		int x, y;
		float localHeight, totalHeight, dhL, dhR, dhT, dhB;
		float dt_A_g_l = dt * A * g / dx; //all constants for equation 2
		float K; // scaling factor for the outﬂow ﬂux
		float totalFlux, h_dxdx_dt;
		float tempFluxL, tempFluxR, tempFluxT, tempFluxB;
		
		
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
				if (totalFlux <= 0.0f || totalFlux <= h_dxdx_dt) {
					tempFlux[x][y].left =	 tempFluxL;  //(5)
					tempFlux[x][y].right =	 tempFluxR;
					tempFlux[x][y].top =	 tempFluxT;
					tempFlux[x][y].bottom =	 tempFluxB;
				}
				else {							//if (totalFlux > 0)
					K = h_dxdx_dt / totalFlux;	//; Mathf.Min(1.0f, h_dxdx_dt / totalFlux);  //(4)
					
					tempFlux[x][y].left =	 K * tempFluxL;  //(5)
					tempFlux[x][y].right =	 K * tempFluxR;
					tempFlux[x][y].top =	 K * tempFluxT;
					tempFlux[x][y].bottom =	 K * tempFluxB;
				}
				//swap temp and the real one after the for-loops
			}
		}
	}
	
	static void UpdateHeight(float dt, float dx, float[][] lowerLayersHeight,
		OutflowFlux[][] tempFlux,
		float[][] height, float[][] tempHeight
		) {
		
		int x, y;
		float dV;
		for (x=1 ; x <= N ; x++ ) {
			for (y=1 ; y <= N ; y++ ) {
				//
				// 3.2.2 Water Surface (and Velocity Field Update)
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
		
	}

	static void UpdateVelocity(float dx,
		OutflowFlux[][] tempFlux,
		float[][] height,
		float[][] tempHeight,
		Velocity[][] tempVelocity
		) {
		int x, y;
		float dWx, dWy;
		float dAv; //dAvarage
		
		for (x=1 ; x <= N ; x++ ) {
			for (y=1 ; y <= N ; y++ ) {
				//
				// 3.2.2 (Water Surface and) Velocity Field Update
				// ----------------------------------------------------------------------------------------
				dWx = (tempFlux[x-1][y].right - tempFlux[x][y].left + tempFlux[x][y].right - tempFlux[x+1][y].left) / 2.0f; //8
				dWy = (tempFlux[x][y-1].top - tempFlux[x][y].bottom + tempFlux[x][x].top - tempFlux[x][y+1].bottom) / 2.0f;
				dAv = (height[x][y] + tempHeight[x][y]) / 2.0f;
				
				tempVelocity[x][y].u = dWx / dx / dAv; //9
				tempVelocity[x][y].v = dWy / dx / dAv; //dx used for ly here since we assume a square grid
				//swap temp and the real one later
			}
		}
	}
	
	
		
	static void UpdateVisuals(float[][] lowerLayersHeight, float opaqueHeight, float nonZeroHeightOffset, float[][] height, Vector3[] vertices, Color32[] colors) {
		int i, j, index;
		float locHeight, relHeight;
		for (i = 0; i < N+2; ++i) {
			for (j = 0; j < N+2; ++j) {
				index = CalculateIndex(i,j);
				locHeight = height[i][j];
				relHeight = locHeight / opaqueHeight;
				
				vertices[index].y = locHeight + lowerLayersHeight[i][j]
					+ (locHeight > 0 ? nonZeroHeightOffset : 0);
				colors[index].a = (byte)Mathf.Lerp(0, 255, relHeight);
			}
		}
	}
	
	

	void OnGUI() {
		GUI.Label(new Rect(10,30, 800, 30), "Update: " + _updateTimer);
		GUI.Label(new Rect(10,50, 800, 30), "Simulation main thread: " + _applyUpdateTimer);
		GUI.Label(new Rect(10,70, 800, 30), "Wait time: " + _waitTimer);
		GUI.Label(new Rect(10,90, 300, 30), "Total Volume: " + _totalVolume.ToString("0.000000000"));
	}

	void OnDrawGizmos () {
		/*if (_vertices != null && _velocity[0] != null) {
			float size = 10;
			float scaleX = size/(N+1);
			float scaleY = size/(N+1);
	
			int x, y;
			for (x=1 ; x <= N ; x++ ) {
				for (y=1 ; y <= N ; y++ ) {
					int index = CalculateIndex(x, y);
					var worldPos = _vertices[index];
					var vel = _velocity[x][y];
					var velVector = new Vector3(vel.u, 0, vel.v) / 100.0f;
					Gizmos.DrawLine(worldPos, worldPos + velVector);
				}
			}
		}*/
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
				uvs[index] = new Vector2(x*uvFactorX, y*uvFactorY);
				colors[index] = new Color32(255, 255, 255, 255);

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
