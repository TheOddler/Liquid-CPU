using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ElementLayerManager : MonoBehaviour {

	public const int N = 128 - 2;
	
	public float _addPower = 0.02f;
	public float _dt = 0.02f;
	public float _dx;
	private float _timeSinceLastUpdate = 0.0f;

	public List<ElementLayer> _layers = new List<ElementLayer>(2);
	public ElementLayer _addToLayer;
	public Collider _colliderToAddOn;

	float[][] _tempTotalHeight = new float[N+2][];
	float[][] _tempSource = new float[N+2][];
	
	Timer _timer = new Timer();

	// Use this for initialization
	void Start () {
		for (int i = 0; i < N+2; ++i) {
			_tempTotalHeight[i] = new float[N+2];
			_tempSource[i] = new float[N+2];
		}
		
		if (_dx <= 0) {
			float Np2 = (float)(N+2);
			_dx = collider.bounds.size.x / Np2;
		}
	}
	
	// Update is called once per frame
	void Update () {
		//
		// First add to/remove from the layer
		// ----------------------------------------------------------------------
		CalculateSources(_colliderToAddOn);
		_addToLayer.AddSource(_tempSource);
		
		
		_timeSinceLastUpdate += Time.deltaTime;
		if (_timeSinceLastUpdate >= _dt) {
			_timeSinceLastUpdate -= _dt;
			
			//
			// Update each layer
			// ----------------------------------------------------------------------
			ResetTotalHeight(); //The first layer just sits on a plane
			for (int i = 0; i < _layers.Count; ++i) {
				_layers[i].DoUpdate(_dt, _dx, _tempTotalHeight);
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

	void CalculateSources(Collider collider) {
		for (int i=0 ; i<N+2 ; i++ ) { 
			for (int j=0 ; j<N+2 ; j++ ) {
				_tempSource[i][j] = 0;
			}
		}

		float mouseModifier = 0;
		if (Input.GetMouseButton(0)) {
			mouseModifier = 1;
		}
		else if (Input.GetMouseButton(1)) {
			mouseModifier = -2;
		}
		
		if (mouseModifier != 0) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			if (collider.Raycast(ray, out hit, float.PositiveInfinity)) {
				Vector2 hitpos = hit.textureCoord * N;
				int x = Mathf.RoundToInt(hitpos.x);
				int y = Mathf.RoundToInt(hitpos.y);
				if (x > 10 && x < N-10 && y > 10 && y < N-10) {
					float add = mouseModifier * _addPower * _dt / _dx / _dx / 4;
					
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
