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
	
	Terrain _terrain;

	// Use this for initialization
	void Start () {
		for (int i = 0; i < N+2; ++i) {
			_height[i] = new float[N+2];
		}
		
		//sample terrain
		_terrain = GetComponent<Terrain>();
		float Np2 = (float)(N+2);
		for (int i = 0 ; i < N+2 ; i++ ) {
			for (int j = 0 ; j < N+2 ; j++ ) {
				Vector3 worldPos = transform.position + new Vector3( i/Np2*10.0f, 0, j/Np2*10.0f); //TODO Get rid of the magic number 10. This is currently the size of a layer
				
				_height[i][j] = _terrain.SampleHeight(worldPos) + _offset;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public override void AddSource(float[][] source) {
	}

	public override void DoUpdate(float dt, float dx, float[][] lowerLayersHeight) {
	}

	public override void ApplyVisuals(float[][] lowerLayersHeight) {
	}
	
}
