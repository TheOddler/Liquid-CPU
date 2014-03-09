using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FluidLayerManager : MonoBehaviour {

	const int N = FluidSolver.N;

	public List<FluidLayer> _layers = new List<FluidLayer>(2);

	float[][]
		//_horSpeedSource = new float[N+2][],
		//_verSpeedSource = new float[N+2][],
		_densitySource = new float[N+2][];

	// Use this for initialization
	void Start () {
		for (int i = 0; i < N+2; ++i) {
			//_horSpeedSource[i] = new float[N+2];
			//_verSpeedSource[i] = new float[N+2];
			_densitySource[i] = new float[N+2];
		}
	}
	
	// Update is called once per frame
	void Update () {
		CalculateSources(0, _layers[0].collider);
		_layers[0].DoUpdate(null, null, _densitySource, null);

		for (int i = 1; i < _layers.Count; ++i) {
			CalculateSources(i, _layers[i].collider);
            _layers[i].DoUpdate(_layers[i-1].HorGradient, _layers[i-1].VerGradient, _densitySource, _layers[i-1].Density);
		}
	}

	void CalculateSources(int mouseKey, Collider collider) {
		for (int i=0 ; i<N+2 ; i++ ) { 
			for (int j=0 ; j<N+2 ; j++ ) {
				_densitySource[i][j] = 0;
				//_horSpeedSource[i][j] = 0; //1.0f;
				//_verSpeedSource[i][j] = 0; //1.0f;
			}
		}
		
		
		/*if (Input.GetMouseButtonDown (mouseKey)) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			if (collider.Raycast(ray, out hit, float.PositiveInfinity)) {
				_prevMouseWorldPos = hit.point;
			}
		}*/
		if (Input.GetMouseButton (mouseKey)) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			if (collider.Raycast(ray, out hit, float.PositiveInfinity)) {
				Vector2 hitpos = (Vector2.one - hit.textureCoord) * N;
				int x = Mathf.RoundToInt(hitpos.x);
				int y = Mathf.RoundToInt(hitpos.y);
				if (x > 10 && x < N-10 && y > 10 && y < N-10) {
					_densitySource[x][y] += 100;
					
					//Vector3 move = (hit.point - _prevMouseWorldPos) * 10000;
					//_horSpeedSource[x][y] += move.x;
					//_verSpeedSource[x][y] += move.z;
					
					//_prevMouseWorldPos = hit.point;
				}
			}
		}
	}
}
