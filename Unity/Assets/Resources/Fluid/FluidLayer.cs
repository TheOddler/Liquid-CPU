using UnityEngine;
using System.Collections;

public class FluidLayer : MonoBehaviour {

	const int N = FluidSolver.N;

	public float _fluidity = 0.0001f;
	public float _diffusion = 0.0001f;

	float[][]
		_horSpeed = new float[N+2][],
		_verSpeed = new float[N+2][],
		_density = new float[N+2][],

		/*_horSpeedSource = new float[N+2][],
		_verSpeedSource = new float[N+2][],
		_densitySource = new float[N+2][],*/
		
		_horGradient = new float[N+2][],
		_verGradient = new float[N+2][];

    public float[][] Density
    {
        get
        {
            return _density;
        }
    }

	public float[][] HorGradient {
		get {
			return _horGradient;
		}
	}

	public float[][] VerGradient {
		get {
			return _verGradient;
		}
	}

	float _totalDens;

	Mesh _mesh;
	//Vector3 _prevMouseWorldPos;

	//Performance test
	Timer _fluidTimer = new Timer();
	//

	// Use this for initialization
	void Start () {
		for (int i = 0; i < N+2; ++i) {
			_horSpeed[i] = new float[N+2];
			_verSpeed[i] = new float[N+2];
			_density[i] = new float[N+2];

			/*_horSpeedSource[i] = new float[N+2];
			_verSpeedSource[i] = new float[N+2];
			_densitySource[i] = new float[N+2];*/

			_horGradient[i] = new float[N+2];
			_verGradient[i] = new float[N+2];
		}

		// Initialize visuals
        //renderer.material.mainTexture = new Texture2D (N+2, N+2, TextureFormat.ARGB32, false);
		_mesh = new Mesh();
		GetComponent<MeshFilter> ().mesh = _mesh;
		//(collider as MeshCollider).sharedMesh = _mesh;
		CreatePlaneMesh (10);
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
	void GenerateTexture() {
		var texture = renderer.material.mainTexture as Texture2D;

		// set the pixel values
		for (int i = 0; i < N+2; ++i) {
			for (int j = 0; j < N+2; ++j) {
				float r = _horGradient[i][j];
				float g = _verGradient[i][j];
				float b = _density[i][j];
				texture.SetPixel(i, j, new Color(r, g, b));
			}
		}
		
		// Apply all SetPixel calls
		texture.Apply();
	}
    void ApplyDepthData(float[][]lowerLayersDens) {
		//var mesh = GetComponent<MeshFilter> ().sharedMesh;
		var mesh = _mesh;
		Vector3[] vertices = mesh.vertices;
		//int[] triangles = _mesh.triangles;

		for (int i = 0; i < N+2; ++i) {
			for (int j = 0; j < N+2; ++j) {
				int index = FluidSolver.CalculateIndex(i,j);
                vertices[index].y = lowerLayersDens != null ? _density[i][j] + lowerLayersDens[i][j] : _density[i][j];
			}
		}

		mesh.vertices = vertices;
		//_mesh.triangles = triangles;
	}
	
	// Update is called once per frame
	void Update () {
		//DoUpdate ();
	}

	public void DoUpdate(float[][] horSpeedSource, float[][] verSpeedSource, float[][]densSource, float[][]lowerLayersDens) {
		FluidUpdate(horSpeedSource, verSpeedSource, densSource);
		
		//GenerateTexture ();
        ApplyDepthData (lowerLayersDens);
	}

	void FluidUpdate(float[][] horSpeedSource, float[][] verSpeedSource, float[][]densSource) {
		float dt = Time.deltaTime;

		for (int i=0 ; i<N+2 ; i++ ) { 
			for (int j=0 ; j<N+2 ; j++ ) {
				_totalDens += _density[i][j];
			}
		}

		_fluidTimer.Start();
		FluidSolver.UpdateFluid(
			N,
			_horSpeed, _verSpeed, _density, _verGradient, _horGradient,
			horSpeedSource, verSpeedSource, densSource,
			_fluidity, _diffusion,
			dt
			);
		_fluidTimer.Stop();
	}

	void OnGUI() {
		GUI.Label(new Rect(10,10, 500, 30), "Fluid: " + _fluidTimer);
		GUI.Label(new Rect(10,30, 500, 30), "Total Density: " + _totalDens);
	}

	void OnDrawGizmosSelected () {
        // Draw a yellow sphere at the transform's position
		for (int i=1; i<=N; ++i) { 
			for (int j=1; j<=N; ++j) {
				Vector3 dir = new Vector3 (_horGradient[i][j], 0, _verGradient[i][j]);
				float speed = dir.magnitude;
				dir /= speed * 10.0f;
				speed /= 10.0f;
				Gizmos.color = new Color(speed, 0, 0);
				Gizmos.DrawRay (
					transform.position -
					new Vector3 (5.0f - 1.0f/(float)(N + 2)*5.0f, 0, 5.0f - 1.0f/(float)(N + 2)*5.0f) +
					new Vector3 ((float)i / (float)(N + 2) * 10.0f, _density[i][j], (float)j / (float)(N + 2) * 10.0f),
					dir);
				/*Gizmos.DrawSphere(
					transform.position -
					new Vector3 (5.0f - 1.0f/(float)(N + 2)*5.0f, 0, 5.0f - 1.0f/(float)(N + 2)*5.0f) +
					new Vector3 ((float)i / (float)(N + 2) * 10.0f, _density[i][j], (float)j / (float)(N + 2) * 10.0f),
					.1f);*/
			}
		}
    }
}
