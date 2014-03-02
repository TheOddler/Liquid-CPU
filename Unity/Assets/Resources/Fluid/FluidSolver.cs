using UnityEngine;
using System.Collections;
using System.Linq;

// Paper used: http://www.dgp.toronto.edu/people/stam/reality/Research/pdf/GDC03.pdf

public class FluidSolver : MonoBehaviour {

	// Performance check
	Timer _fluidTimer = new Timer ();
	Timer _visualsTimer = new Timer ();
	Timer _extraTimer = new Timer();
	Timer _diffuseTimer = new Timer();
	// 

	const int N = 100;
	const int size=(N+2)*(N+2);

	float[][]
	u = new float[N+2][],
	v = new float[N+2][],
	u_prev = new float[N+2][],
	v_prev = new float[N+2][],
		
	dens = new float[N+2][],
	dens_prev = new float[N+2][];

	float[][]
	u_source = new float[N+2][],
	v_source = new float[N+2][],
	dens_source = new float[N+2][];
    
	Mesh _mesh;
	Vector3 _prevMouseWorldPos;

	// Use this for initialization
	void Start () {
		// Initialize all jagged arrays
		for (int i = 0; i < N+2; ++i) {
			u[i] = new float[N+2];
			v[i] = new float[N+2];
			u_prev[i] = new float[N+2];
			v_prev[i] = new float[N+2];

			dens[i] = new float[N+2];
			dens_prev[i] = new float[N+2];

			u_source[i] = new float[N+2];
			v_source[i] = new float[N+2];
			dens_source[i] = new float[N+2];
		}

		// Add some test-density
		for (int i=1 ; i<=N ; i++ ) { 
			for (int j=1 ; j<=N ; j++ ) {
				dens[i][j] = 0;//Mathf.Max( ((float)(j)*2/(float)N)-1, 0);
			}
		}
        
		// Initialize visuals
        renderer.material.mainTexture = new Texture2D (N+2, N+2, TextureFormat.ARGB32, false);
		_mesh = new Mesh();
		GetComponent<MeshFilter> ().mesh = _mesh;
		//(collider as MeshCollider).sharedMesh = _mesh;
		CreatePlaneMesh (10);
	}
	
	// Update is called once per frame
	void Update () {
		_fluidTimer.Start ();
		FluidUpdate ();
		_fluidTimer.Stop ();

		_visualsTimer.Start ();
		GenerateTexture ();
		ApplyDepthData ();
		_visualsTimer.Stop ();
	}

	void OnGUI () {
		GUI.Label (new Rect (10, 10, 600, 20), "Fluid " + _fluidTimer);
		GUI.Label (new Rect (10, 30, 600, 20), "Visua " + _visualsTimer);
		GUI.Label (new Rect (10, 70, 600, 20), "Extra " + _extraTimer);
		GUI.Label (new Rect (10, 90, 600, 20), "Diffu " + _diffuseTimer);
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
		m.RecalculateNormals();
	}

	void ApplyDepthData() {
		//var mesh = GetComponent<MeshFilter> ().sharedMesh;
		var mesh = _mesh;
		Vector3[] vertices = mesh.vertices;
		//int[] triangles = _mesh.triangles;

		for (int i = 0; i < N+2; ++i) {
			for (int j = 0; j < N+2; ++j) {
				int index = CalculateIndex(i,j);
				vertices[index].y = dens[i][j];
			}
		}

		mesh.vertices = vertices;
		//_mesh.triangles = triangles;
	}

	void GenerateTexture() {
		var texture = renderer.material.mainTexture as Texture2D;

		// set the pixel values
		for (int i = 0; i < N+2; ++i) {
			for (int j = 0; j < N+2; ++j) {
				float r = 0; //u[i][j]/2;
				float g = 0; //v[i][j]/2;
				float b = dens[i][j];
				texture.SetPixel(i, j, new Color(r, g, b));
			}
		}
		
		// Apply all SetPixel calls
		texture.Apply();
	}

	void FluidUpdate() {
		float dt = Time.deltaTime;

		for (int i=1 ; i<N+1 ; i++ ) { 
			for (int j=1 ; j<N+1 ; j++ ) {
				dens_prev[i][j] = 0;
				//u_prev[i][j] = 1.0f;
				v_prev[i][j] = 1.0f;
            }
        }

		if (Input.GetMouseButtonDown (0)) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			if (collider.Raycast(ray, out hit, float.PositiveInfinity)) {
				_prevMouseWorldPos = hit.point;
			}
		}
		if (Input.GetMouseButton (0)) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			if (collider.Raycast(ray, out hit, float.PositiveInfinity)) {
				Vector2 hitpos = (Vector2.one - hit.textureCoord) * N;
				int x = Mathf.RoundToInt(hitpos.x);
				int y = Mathf.RoundToInt(hitpos.y);
				dens_prev[x][y] += 100;

				Vector3 move = (hit.point - _prevMouseWorldPos) * 10000;
				u_prev[x][y] += move.x;
				v_prev[x][y] += move.z;

				_prevMouseWorldPos = hit.point;
			}
		}

		float visc = 0.0001f;
		VelocityStep (N, u, v, u_prev, v_prev, visc, dt);

		float diff = .00f; //Diffusion, how well the particles spread out
		DensityStep (N, dens, dens_prev, u, v, diff, dt);
	}
	
	void DensityStep (int N, float[][] x, float[][] x0, float[][] u, float[][] v, float diff, float dt ) {
		//_extraTimer.Start (); 8.6

		//_extraTimer.Start (); 0.07
		AddSource ( N, x, x0, dt );
		//_extraTimer.Stop ();

		Swap (ref x0, ref x );
		//_extraTimer.Start (); //7.8
		Diffuse ( N, 0, x, x0, diff, dt );
		//_extraTimer.Stop ();

		Swap (ref x0, ref x );
		//_extraTimer.Start (); //0.66
		Advect ( N, 0, x, x0, u, v, dt ); 
		//_extraTimer.Stop ();

		//_extraTimer.Stop ();
	}

	
	void VelocityStep (int N, float[][] u, float[][] v, float[][] u0, float[][] v0, float visc, float dt ) { 
		//_extraTimer.Start (); //33.2

		//_extraTimer.Start (); 0.14
		AddSource ( N, u, u0, dt );
		AddSource ( N, v, v0, dt );
		//_extraTimer.Stop ();

		//_extraTimer.Start (); //15.4
		Swap (ref u0, ref u );
		Diffuse ( N, 1, u, u0, visc, dt ); 

		Swap (ref v0, ref v );
		Diffuse ( N, 2, v, v0, visc, dt );
		//_extraTimer.Stop ();

		//_extraTimer.Start (); //7.9
		project ( N, u, v, u0, v0 ); 
		//_extraTimer.Stop ();

		Swap (ref u0, ref u );
		Swap (ref v0, ref v );

		//_extraTimer.Start (); //1.3
		Advect ( N, 1, u, u0, u0, v0, dt );
		Advect ( N, 2, v, v0, u0, v0, dt ); 
		//_extraTimer.Stop ();

		//_extraTimer.Start (); //8
		project ( N, u, v, u0, v0 ); 
		//_extraTimer.Stop ();

		//_extraTimer.Stop ();
	} 

	void Diffuse ( int N, int b, float[][] x, float[][] x0, float diff, float dt ) {
		_diffuseTimer.Start (); //7.8 ; 7.2 ; 

		float a=dt*diff*N*N;
		
		for (int k=0 ; k<20 ; ++k ) {
			//_extraTimer.Start(); 0.3
			for (int i=1 ; i<=N ; ++i ) { 
				for (int j=1 ; j<=N ; ++j ) { 
					x[i][j] =
						(
							x0[i][j] +
							a*(
							x[i-1][j] +
							x[i+1][j] +
							x[i][j-1] +
							x[i][j+1]
							)
						)/(1+4*a); 
				} 
			}
			//_extraTimer.Stop();

			SetBound ( N, b, x );

		}

		_diffuseTimer.Stop ();
	}
	
	void Advect ( int N, int b, float[][] d, float[][] d0, float[][] u, float[][] v, float dt ) { 
		int i0, j0, i1, j1; 
		float x, y, s0, t0, s1, t1, dt0;

		dt0 = dt*N;
		for (int i=1 ; i<=N ; i++ ) { 
			for (int j=1 ; j<=N ; j++ ) {
				x = i-dt0*u[i][j];
				y = j-dt0*v[i][j]; 

				if (x<0.5f) x= 0.5f;
				if (x>N+0.5f) x= N+0.5f;
				i0=(int)x;
				i1=i0+1;

				if (y<0.5f) y= 0.5f;
				if (y>N+0.5f) y= N+0.5f;
				j0=(int)y;
				j1=j0+1;

				s1 = x-i0;
				s0 = 1-s1;
				t1 = y-j0;
				t0 = 1-t1;

				float dOri = d[i][j];
				d[i][j] = 
					s0*(
						t0*d0[i0][j0] +
						t1*d0[i0][j1]
						)
						+
					s1*(
						t0*d0[i1][j0] +
						t1*d0[i1][j1]
						);
				/*if (dOri != d[i][j]) {
					Debug.Log("d changed: " + (dOri - d[i][j]));
				}*/
			} 
		} 
		SetBound ( N, b, d );
	}


	//
	// Helpers
	void AddSource ( int N, float[][] x, float[][] s, float dt ) { 
		//int size=(N+2)*(N+2);
		for (int i=0 ; i<N+2 ; i++ ) { 
			for (int j=0 ; j<N+2 ; j++ ) { 
				x [i][j] += dt * s [i][j];
			}
		}
	} 

	void project ( int N, float[][] u, float[][] v, float[][] p, float[][] div ) {
		float h; 
		h = 1.0f/N; 
		for (int i=1 ; i<=N ; i++ ) { 
			for (int j=1 ; j<=N ; j++ ) { 
				div[i][j] = -0.5f*h*(u[i+1][j]-u[i-1][j]+v[i][j+1]-v[i][j-1]); 
				p[i][j] = 0; 
			} 
		} 
		SetBound ( N, 0, div ); SetBound ( N, 0, p ); 
		
		for (int k=0 ; k<20 ; k++ ) { 
			for (int i=1 ; i<=N ; i++ ) { 
				for (int j=1 ; j<=N ; j++ ) { 
					p[i][j] = (div[i][j]+p[i-1][j]+p[i+1][j]+ p[i][j-1]+p[i][j+1])/4; 
				} 
			} 
			SetBound ( N, 0, p ); 
		} 
		
		for (int i=1 ; i<=N ; i++ ) { 
			for (int j=1 ; j<=N ; j++ ) { 
				u[i][j] -= 0.5f*(p[i+1][j]-p[i-1][j])/h; 
				v[i][j] -= 0.5f*(p[i][j+1]-p[i][j-1])/h; 
			} 
		} 
		SetBound ( N, 1, u ); SetBound ( N, 2, v ); 
	}
	
	void SetBound ( int N, int b, float[][] x ) {
		for (int i=1 ; i<=N ; i++ ) { 
			x[0][i] =	b==1 ? -x[1][i] : x[1][i];
			x[N+1][i] =	b==1 ? -x[N][i] : x[N][i]; 
			x[i][0] =	b==2 ? -x[i][1] : x[i][1]; 
			x[i][N+1] =	b==2 ? -x[i][N] : x[i][N];
		} 
		x[0][0] = 0.5f*(x[1][0]+x[0][1]); 
		x[0][N+1] = 0.5f*(x[1][N+1]+x[0][N]); 
		x[N+1][0] = 0.5f*(x[N][0]+x[N+1][1]); 
		x[N+1][N+1] = 0.5f*(x[N][N+1]+x[N+1][N]);
	} 

	int CalculateIndex(int i, int j) {
		return ((i) + (N + 2) * (j));
	}

	void Swap(ref float[][] x0, ref float[][] x) {
		var tmp=x0;
		x0=x;
		x=tmp;
	} 
}
