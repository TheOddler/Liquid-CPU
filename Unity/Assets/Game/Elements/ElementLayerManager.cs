using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ElementLayerManager : MonoBehaviour {

	public const int N = 200;
	
	public float _addPower = 10.0f;
	public float _dt = 0.02f;
	private float _timeSinceLastUpdate = 0.0f;

	public List<ElementLayer> _layers = new List<ElementLayer>(2);

	float[][] _tempTotalHeight = new float[N+2][];
	float[][] _tempSource = new float[N+2][];
	
	Timer _timer = new Timer();

	// Use this for initialization
	void Start () {
		for (int i = 0; i < N+2; ++i) {
			_tempTotalHeight[i] = new float[N+2];
			_tempSource[i] = new float[N+2];
		}
	}
	
	// Update is called once per frame
	void Update () {
		//
		// First add to/remove from each layer
		// ----------------------------------------------------------------------
		for (int i = 0; i < _layers.Count; ++i) {
			CalculateSources(i, collider);
			_layers[i].AddSource(_tempSource);
		}
		
		
		_timeSinceLastUpdate += Time.deltaTime;
		if (_timeSinceLastUpdate >= _dt) {
			_timeSinceLastUpdate -= _dt;
			
			//
			// Update each layer
			// ----------------------------------------------------------------------
			ResetTotalHeight(); //The first layer just sits on a plane
			float Np2 = (float)(N+2);
			float dx = collider.bounds.size.x / Np2;
			for (int i = 0; i < _layers.Count; ++i) {
				_layers[i].DoUpdate(_dt, dx, _tempTotalHeight);
				_layers[i].ApplyVisuals(_tempTotalHeight);
				AddHeightToTotal(_layers[i].Height);
			}
		}
	}
	
	void ResetTotalHeight() {
		for (int i=0 ; i<N+2 ; i++ ) { 
			for (int j=0 ; j<N+2 ; j++ ) {
				_tempTotalHeight[i][j] = 0;
			}
		}
	}
	
	void AddHeightToTotal(float[][] height) {
		for (int i=0 ; i<N+2 ; i++ ) { 
			for (int j=0 ; j<N+2 ; j++ ) {
				_tempTotalHeight[i][j] += height[i][j];
			}
		}
	}

	void CalculateSources(int mouseKey, Collider collider) {
		for (int i=0 ; i<N+2 ; i++ ) { 
			for (int j=0 ; j<N+2 ; j++ ) {
				_tempSource[i][j] = 0;
			}
		}

		if (Input.GetMouseButton (mouseKey)) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			if (collider.Raycast(ray, out hit, float.PositiveInfinity)) {
				Vector2 hitpos = (Vector2.one - hit.textureCoord) * N;
				int x = Mathf.RoundToInt(hitpos.x);
				int y = Mathf.RoundToInt(hitpos.y);
				if (x > 10 && x < N-10 && y > 10 && y < N-10) {
					float add = _addPower * Time.fixedDeltaTime;
					_tempSource[x][y] += add;
					_tempSource[x][y+1] += add;
					_tempSource[x+1][y] += add;
					_tempSource[x+1][y+1] += add;
				}
			}
		}
	}
	
	
	
	
	
	void OnGUI() {
		GUI.Label(new Rect(10, 10, 500, 30), "Frames behind: " + Mathf.FloorToInt(_timeSinceLastUpdate / _dt));
	}
}
