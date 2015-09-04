using UnityEngine;
using System.Collections;

public class TerrainLayer : ElementLayer {
	const int N = ElementLayerManager.N;
	
	public float _offset = -0.001f;

	private float[][] _height = new float[N+2][];
	public override float[][] HeightField {
		get {
			return _height;
		}
	}
	
	//
	// Visuals
	// -----------------------
	Terrain _terrain;
	Mesh _mesh;
	Vector3[] _vertices;

	void Awake () {
		for (int i = 0; i < N+2; ++i) {
			_height[i] = new float[N+2];
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	public override void Initialize(float dt, float dx, float[][] lowerLayersHeight) {
		//
		// Initialize visuals
		// ----------------------------------------------
		_mesh = new Mesh();
		GetComponent<MeshFilter> ().mesh = _mesh;
		Helpers.CreatePlaneMesh(_mesh, N, 10, new Vector3(5, 0, 5), new Color32(255, 255, 255, 255));
		_vertices = _mesh.vertices;
		
		//
		// Sample terrain
		// --------------------------------------
		_terrain = GetComponent<Terrain>();
		float Np2 = (float)(N+2);
		for (int i = 0 ; i < N+2 ; i++ ) {
			for (int j = 0 ; j < N+2 ; j++ ) {
				Vector3 worldPos = transform.position + new Vector3( (i+0.5f)/Np2*10.0f, 0, (j+0.5f)/Np2*10.0f); //TODO Get rid of the magic number 10. This is currently the size of a layer
				
				_height[i][j] = _terrain.SampleHeight(worldPos) + _offset;
				/*(float)Mathf.Max(
					Mathf.Abs(i - N/2),
					Mathf.Abs(j - N/2))
					/ 20.0f;//_terrain.SampleHeight(worldPos) + _offset;*/
			}
		}
		
		//
		// Hide terrain and apply to vertices
		// ----------------------------------------------------------------------------
		UpdateVisuals(lowerLayersHeight, _height, _vertices);
		_mesh.vertices = _vertices;
		_mesh.RecalculateNormals();
		GetComponent<Terrain>().enabled = false;
	}

	public override void AddSource(float[][] source) {
		for (int i=1 ; i<=N ; i++ ) {
			for (int j=1 ; j<=N ; j++ ) {
				_height[i][j] = Mathf.Max(0, _height[i][j] + source[i][j]);
			}
		}
	}
	
	public override void DoUpdate(float dt, float dx, float[][] lowerLayersHeight) {
		UpdateVisuals(lowerLayersHeight, _height, _vertices);
		_mesh.vertices = _vertices;
		_mesh.RecalculateNormals();
	}
	
	static void UpdateVisuals(float[][] lowerLayersHeight, float[][] height, Vector3[] vertices) {
		int i, j, index;
		for (i = 0; i < N+2; ++i) {
			for (j = 0; j < N+2; ++j) {
				index = CalculateIndex(i,j);
				vertices[index].y = height[i][j] + lowerLayersHeight[i][j];
			}
		}
	}
	
	public static int CalculateIndex(int i, int j) {
		return ((i) + (N + 2) * (j));
	}
}
