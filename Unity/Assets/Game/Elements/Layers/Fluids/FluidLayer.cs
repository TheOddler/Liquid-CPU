using UnityEngine;
using System.Collections;
using System.Threading;

public struct OutflowFlux {
	public float top, bottom, left, right;
	
	public override string ToString() {
		return "t " + top + "; b " + bottom + "; l " + left + "; r " + right;
	}
}

public struct Velocity {
	public float u, v;
	
	public override string ToString() {
		return u.ToString("00.000") + "," + v.ToString("00.000");
	}
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
	public float _damping = 1.0f;
	public float _initialHeight = 0.1f;
	public float _opaqueHeight = 0.5f;
	public ElementLayer _toErode;

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
	Color32[][] _pigment = new Color32[N+2][];
	
	float[][] _sediment = new float[N+2][];
	float[][] _erosionDeposition = new float[N+2][];

	//
	// Temporary values
	// -------------------------------------------
	float[][] _tempLowerLayersHeight = new float[N+2][];
	float[][] _tempHeight = new float[N+2][];
	OutflowFlux[][] _tempFlux = new OutflowFlux[N+2][];
	float[][] _tempSource = new float[N+2][];
	
	Velocity[][] _tempVelocity = new Velocity[N+2][];
	
	Color32[][] _tempPigment = new Color32[N+2][];
	
	float[][] _tempSediment = new float[N+2][];
	float[][] _tempErosionDeposition = new float[N+2][];

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

	bool _showDetails = false;
	Timer _heightTimer = new Timer();
	Timer _fluxTimer = new Timer();
	Timer _sourceTimer = new Timer();
	Timer _velocityTimer = new Timer();
	Timer _visualsTimer = new Timer();
	Timer _erosionDepositionTimer = new Timer();
	Timer _sedimentTransportTimer = new Timer();
	

	void Awake() {
		//
		// Init simulated variables
		// -------------------------------------------
		for (int i = 0; i < N+2; ++i) {
			_height[i] = new float[N+2];
			_flux[i] = new OutflowFlux[N+2];
			_source[i] = new float[N+2];

			_velocity[i] = new Velocity[N+2];

			_pigment[i] = new Color32[N+2];
			
			_sediment[i] = new float[N+2];
			_erosionDeposition[i] = new float[N+2];

			// temporary
			_tempLowerLayersHeight[i] = new float[N+2];
			_tempHeight[i] = new float[N+2];
			_tempSource[i] = new float[N+2];
			_tempFlux[i] = new OutflowFlux[N+2];

			_tempVelocity[i] = new Velocity[N+2];
			
			_tempPigment[i] = new Color32[N+2];
			
			_tempSediment[i] = new float[N+2];
			_tempErosionDeposition[i] = new float[N+2];
		}
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
		// Initialize visuals
		// ----------------------------------------------
		_mesh = new Mesh();
		GetComponent<MeshFilter> ().mesh = _mesh;
		Helpers.CreatePlaneMesh(_mesh, N, 10, Vector3.zero, new Color(255, 255, 255, 0));
		_vertices = _mesh.vertices;
		_colors = _mesh.colors32;

		//
		// Set initial fluid height
		// -----------------------------------------------
		for (int i=1 ; i<=N ; i++ ) {
			for (int j=1 ; j<=N ; j++ ) {
				_height[i][j] = _tempHeight[i][j] = _initialHeight;
			}
		}
		
		//
		// Set initial pigment
		// -----------------------------------------------
		for (int i=1 ; i<=N ; i++ ) {
			for (int j=1 ; j<=N ; j++ ) {
				_pigment[i][j] = _tempPigment[i][j] = 
					new Color(
						(float)i/(float)N,
						(float)j/(float)N,
						0
					);
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
		Helpers.Swap(ref _tempPigment, ref _pigment);
		Helpers.Swap(ref _tempSediment, ref _sediment);
		Helpers.Swap(ref _tempErosionDeposition, ref _erosionDeposition);
		_mesh.vertices = _vertices;
		_mesh.colors32 = _colors;
		//Start a new update for the next time DoUpdate is called.
		DoUpdateThreaded(dt, dx, lowerLayersHeight);
		_toErode.AddSource(_erosionDeposition);

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

			_sourceTimer.Start();
			DoAddSourceToHeight(_tempSource, _height);
			_sourceTimer.Stop();

			_fluxTimer.Start();
			UpdateFlux(dt, dx, _tempLowerLayersHeight, _A, _damping, _flux, _tempFlux, _height);
			_fluxTimer.Stop();

			_heightTimer.Start();
			UpdateHeight(dt, dx, _tempLowerLayersHeight, _tempFlux, _height, _tempHeight);
			_heightTimer.Stop();

			_velocityTimer.Start();
			UpdateVelocity(dx, _tempFlux, _height, _tempHeight, _tempVelocity);
			_velocityTimer.Stop();
			
			//UpdatePigment(dt, _pigment, _tempPigment, _velocity);
			
			_erosionDepositionTimer.Start();
			UpdateErosionDeposition(dt, dx, 1.5f, 0.5f, 0.3f, _velocity, _height, _tempLowerLayersHeight, _sediment, _tempErosionDeposition);
			_erosionDepositionTimer.Stop();
			
			_sedimentTransportTimer.Start();
			UpdateSedimentTransportation(dt, _sediment, _tempSediment, _velocity);
			_sedimentTransportTimer.Stop();
				
			UpdateEvaporation(dt, 0.1f, _tempHeight);

			_visualsTimer.Start();
			UpdateVisuals(_tempLowerLayersHeight, _opaqueHeight, _height, _pigment, _sediment, _vertices, _colors);
			_visualsTimer.Stop();

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
		float A, float damping,
		OutflowFlux[][] flux, OutflowFlux[][] tempFlux,
		float[][] height
		) {
		int x, y;
		float localHeight, totalHeight, dhL, dhR, dhT, dhB;
		float dt_A_g_l = dt * A * g / dx; //all constants for equation 2
		float K; // scaling factor for the outﬂow ﬂux
		float totalFlux, h_dx_dx_dt;
		float tempFluxL, tempFluxR, tempFluxT, tempFluxB;
		float fluxDamp = 1.0f - dt * damping;

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

				/*tempFluxL = Mathf.Max(0.0f, flux[x][y].left		+ dt_A_g_l * dhL ); //(2)
				tempFluxR = Mathf.Max(0.0f, flux[x][y].right	+ dt_A_g_l * dhR );
				tempFluxT = Mathf.Max(0.0f, flux[x][y].top		+ dt_A_g_l * dhT );
				tempFluxB = Mathf.Max(0.0f, flux[x][y].bottom	+ dt_A_g_l * dhB );*/

				tempFluxL = flux[x][y].left		* fluxDamp + dt_A_g_l * dhL;		tempFluxL = tempFluxL > 0 ? tempFluxL : 0; //(2)
				tempFluxR = flux[x][y].right	* fluxDamp + dt_A_g_l * dhR;		tempFluxR = tempFluxR > 0 ? tempFluxR : 0;
				tempFluxT = flux[x][y].top		* fluxDamp + dt_A_g_l * dhT;		tempFluxT = tempFluxT > 0 ? tempFluxT : 0;
				tempFluxB = flux[x][y].bottom	* fluxDamp + dt_A_g_l * dhB;		tempFluxB = tempFluxB > 0 ? tempFluxB : 0;

				totalFlux = tempFluxL + tempFluxR + tempFluxT + tempFluxB;
				h_dx_dx_dt = localHeight * dx * dx / dt;
				if (totalFlux <= 0.0f || totalFlux <= h_dx_dx_dt) {
					tempFlux[x][y].left =	 tempFluxL;  //(5)
					tempFlux[x][y].right =	 tempFluxR;
					tempFlux[x][y].top =	 tempFluxT;
					tempFlux[x][y].bottom =	 tempFluxB;
				}
				else {							//if (totalFlux > 0)
					K = h_dx_dx_dt / totalFlux;	//; Mathf.Min(1.0f, h_dxdx_dt / totalFlux);  //(4)

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
		float dx_dx_Inv = 1.0f / (dx * dx);
		float dV;
		OutflowFlux locFlux;
		for (x=1 ; x <= N ; x++ ) {
			for (y=1 ; y <= N ; y++ ) {
				//
				// 3.2.2 Water Surface (and Velocity Field Update)
				// ----------------------------------------------------------------------------------------
				locFlux = tempFlux[x][y];
				dV = dt * (
					//sum in
					tempFlux[x-1][y].right + tempFlux[x][y-1].top + tempFlux[x+1][y].left + tempFlux[x][y+1].bottom
					//minus sum out
					- locFlux.right - locFlux.top - locFlux.left - locFlux.bottom
					); //(6)
				tempHeight[x][y] = height[x][y] + dV * dx_dx_Inv; //(7)
				//swap temp and the real one after the for-loops
			}
		}
	}
	
	static void UpdateErosionDeposition(float dt, float dx, float Kc, float Ks, float Kd,
		Velocity[][] curVelocity,
		float[][] fluidHeight,
		float[][] lowerLayersHeight,
		float[][] curSediment,
		float[][] toErodeDeposit
	) {
		int x, y;
		Velocity v;
		float C;
		float st;
		float Kc_dxdx = Kc * dx * dx;
		float sinAlpha, dhx, dhy;
		float dx2inv = 1.0f / (2.0f * dx);
		float temp;
		for (x=1 ; x <= N ; x++ ) {
			for (y=1 ; y <= N ; y++ ) {
				//
				// 3.3 Erosion and Deposition
				// --------------------------------------------------------------
				v = curVelocity[x][y];
				// calculations for tilt with help from: http://math.stackexchange.com/questions/1044044/local-tilt-angle-based-on-height-field#1044080
				dhx = (fluidHeight[x+1][y] - fluidHeight[x-1][y]) * dx2inv;
				dhy = (fluidHeight[x][y+1] - fluidHeight[x][y-1]) * dx2inv;
				sinAlpha = Mathf.Sqrt(1 - 1 / (1 + dhx*dhx + dhy*dhy));
				C = Kc_dxdx * sinAlpha * Mathf.Sqrt(v.u*v.u + v.v*v.v);// * fluidHeight[x][y];
				st = curSediment[x][y];
				if (C > st) {
					temp = Mathf.Min(C - st, Ks * (C - st));
					toErodeDeposit[x][y] = -temp;
					curSediment[x][y] = st + temp;
				}
				else {
					temp = Mathf.Max(st, Kd * (st - C));
					toErodeDeposit[x][y] = temp;
					curSediment[x][y] = st - temp;
				}
			}
		}
	}
	
	static void UpdateSedimentTransportation(float dt,
		float[][] curSediment, float[][] nextSediment, 
		Velocity[][] velocity
	) {
		int i, j;
		int i0, i1, j0, j1;
		float s1, t1, s0, t0;
		float dt0 = dt*N;
		float x, y;
		//float nextSed;
		for (i=1 ; i <= N ; ++i ) {
			for (j=1 ; j <= N ; ++j ) {
				x = i - dt0*velocity[i][j].u;
				y = j - dt0*velocity[i][j].v;
				if (x < 0.5f)		x = 0.9f;
				if (x > N + 0.5f)	x = N+0.1f;
				i0 = (int)x;
				i1 = i0+1;
				
				if (y < 0.5f)		y = 0.9f;
				if (y > N + 0.5f)	y = N+0.1f;
				j0 = (int)y;
				j1 = j0+1;
				s1 = x - i0;
				s0 = 1 - s1;
				t1 = y - j0;
				t0 = 1 - t1;
				nextSediment[i][j] =
					s0 * (t0 * curSediment[i0][j0] + t1 * curSediment[i0][j1]) +
					s1 * (t0 * curSediment[i1][j0] + t1 * curSediment[i1][j1]) ;
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
		float dxInv = 1/dx;
			
		for (x=1 ; x <= N ; x++ ) {
			for (y=1 ; y <= N ; y++ ) {
				//
				// 3.2.2 (Water Surface and) Velocity Field Update
				// ----------------------------------------------------------------------------------------
				dAv = Mathf.Max(0.000001f, (height[x][y] + tempHeight[x][y]) * 0.5f);
				
				dWx = (tempFlux[x-1][y].right	- tempFlux[x][y].left	+ tempFlux[x][y].right	- tempFlux[x+1][y].left)	* 0.5f; //8
				dWy = (tempFlux[x][y-1].top 	- tempFlux[x][y].bottom + tempFlux[x][y].top	- tempFlux[x][y+1].bottom)	* 0.5f;
				
				tempVelocity[x][y].u = dWx * dxInv / dAv; //9
				tempVelocity[x][y].v = dWy * dxInv / dAv; //dx used for ly here since we assume a square grid
				//swap temp and the real one later
			}
		}
	}
	
	static void UpdatePigment(float dt,
		Color32[][] pigment, Color32[][] tempPigment, 
		Velocity[][] velocity
	) {
		int i, j;
		int i0, i1, j0, j1;
		float s1, t1; //s0, t0
		float dt0 = dt*N;
		float x, y;
		for (i=1 ; i <= N ; ++i ) {
			for (j=1 ; j <= N ; ++j ) {
				x = i - dt0*velocity[i][j].u;
				y = j - dt0*velocity[i][j].v;
				if (x < 0.5f)		x = 0.5f;
				if (x > N + 0.5f)	x = N+0.5f;
				i0 = (int)x;
				i1 = i0+1;
				
				if (y < 0.5f)		y = 0.5f;
				if (y > N + 0.5f)	y = N+0.5f;
				j0 = (int)y;
				j1 = j0+1;
				s1 = x - i0;
				//s0 = 1 - s1;
				t1 = y - j0;
				//t0 = 1 - t1;
				tempPigment[i][j] = 
					Color32.Lerp(
					Color32.Lerp(pigment[i0][j0], pigment[i0][j1], t1),
					Color32.Lerp(pigment[i1][j0], pigment[i1][j1], t1),
					s1
					);
			}
		}
	}
	
	static void UpdateEvaporation(float dt, float Ke, float[][] waterHeight) {
		int i, j;
		float one_Ke_dt = 1 - Ke * dt;
		for (i = 0; i < N+2; ++i) {
			for (j = 0; j < N+2; ++j) {
				waterHeight[i][j] *= one_Ke_dt;
			}
		}
	}


	static void UpdateVisuals(float[][] lowerLayersHeight, float opaqueHeight, float[][] height, Color32[][] pigment, float[][] sediment, Vector3[] vertices, Color32[] colors) {
		int i, j, index;
		float locHeight;
		for (i = 0; i < N+2; ++i) {
			for (j = 0; j < N+2; ++j) {
				index = CalculateIndex(i,j);
				locHeight = height[i][j];

				vertices[index].y = locHeight + lowerLayersHeight[i][j];
				colors[index] = new Color32(
					255,
					(byte)(Mathf.Max(0, 255.0f - sediment[i][j] * 255.0f * 1000.0f)),
					(byte)(Mathf.Max(0, 255.0f - sediment[i][j] * 255.0f * 1000.0f)),
					255); //pigment[i][j];
				colors[index].a = opaqueHeight > locHeight ? (byte)(255.0f * locHeight / opaqueHeight) : byte.MaxValue;
			}
		}
	}



	void OnGUI() {
		GUI.Label(new Rect(10,30, 800, 30), "Update:\t\t" + _updateTimer);
		GUI.Label(new Rect(10,50, 800, 30), "Wait time:\t\t" + _waitTimer);
		GUI.Label(new Rect(10,70, 800, 30), "Main thread:\t" + _applyUpdateTimer);
		GUI.Label(new Rect(10,90, 300, 30), "Total Volume: " + _totalVolume.ToString("0.000000000"));

		_showDetails = GUI.Toggle(new Rect(10,110, 100, 30), _showDetails, "Show Details");
		if (_showDetails) {
			GUI.Label(new Rect(10,130, 800, 30), "Source:\t" + _sourceTimer);
			GUI.Label(new Rect(10,150, 800, 30), "Flux:\t" + _fluxTimer);
			GUI.Label(new Rect(10,170, 800, 30), "Height:\t" + _heightTimer);
			GUI.Label(new Rect(10,190, 800, 30), "Velocity:\t" + _velocityTimer);
			GUI.Label(new Rect(10,210, 800, 30), "Erosion:\t" + _erosionDepositionTimer);
			GUI.Label(new Rect(10,230, 800, 30), "Sediment:\t" + _sedimentTransportTimer);
			GUI.Label(new Rect(10,250, 800, 30), "Visuals:\t" + _visualsTimer);
		}
	}

	void OnDrawGizmos () {
		if (_vertices != null && _velocity[0] != null) {
			int x, y;
			for (x=1 ; x <= N ; x++ ) {
				for (y=1 ; y <= N ; y++ ) {
					int index = CalculateIndex(x, y);
					var worldPos = _vertices[index];
					
					var vel = _velocity[x][y];
					var velVec = new Vector3(vel.u, 0, vel.v); // * _height[x][y];
					var velVecNor = velVec.normalized;
					Gizmos.color = new Color(.5f + .5f * velVecNor.x,  .5f + .5f * velVecNor.z, 0.5f);
					Gizmos.DrawRay(worldPos, velVec);
					
					/*OutflowFlux flux = _flux[x][y];
					Gizmos.color = Color.red;
					Gizmos.DrawRay(worldPos, new Vector3(flux.right * 500.0f, 0, 0));
					Gizmos.color = Color.green;
					Gizmos.DrawRay(worldPos, new Vector3(-flux.left * 500.0f, 0, 0));
					Gizmos.color = Color.blue;
					Gizmos.DrawRay(worldPos, new Vector3(0, 0, flux.top * 500.0f));
					Gizmos.color = Color.white;
					Gizmos.DrawRay(worldPos, new Vector3(0, 0, -flux.bottom * 500.0f));*/
				}
			}
		}
	}



	public static int CalculateIndex(int i, int j) {
		return ((i) + (N + 2) * (j));
	}
}
