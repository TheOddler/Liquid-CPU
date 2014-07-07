using UnityEngine;
using System.Collections;

public class Boat : MonoBehaviour {
	
	public ElementLayerManager _elementManager;
	public FluidLayer _fluidLayer;
	
	public float _velocityToForceMultiplyer = 2.0f;
	public float _buoyancyMultiplyer = 50.0f;
	public float _bottomOffest = -0.35f;
	public float _minimumWaterForBuoyancy = 0.2f;
	
	Collider _collider;
	Rigidbody _rigidbody;
	
	void Start() {
		_rigidbody = GetComponent<Rigidbody>();
		_collider = GetComponent<Collider>();
	}
	
	public void Initialize(ElementLayerManager manager, FluidLayer fluid) {
		_elementManager = manager;
		_fluidLayer = fluid;
	}
	
	void FixedUpdate() {
		//Movement
		CalculateAndAddForceToPoint(new Vector3(0, 0, 0.22f));
		CalculateAndAddForceToPoint(new Vector3(0, 0, -0.18f));
		
		//buoyancy
		GridPoint point = _elementManager.GridPointFromPosition(transform.position, true);
		if (point.x >= 0 && point.y >= 0) {
			float waterHeight = _fluidLayer.HeightField[point.x][point.y];
			
			float height = _elementManager.CurrentTotalHeight[point.x][point.y];
			float bottom = _collider.bounds.center.y - _collider.bounds.extents.y + _bottomOffest;
			float buoyancy = (height - bottom) * _buoyancyMultiplyer * Mathf.Lerp(0, 1, waterHeight / _minimumWaterForBuoyancy);
			
		 	_rigidbody.AddForce(
		 		0,
				buoyancy,
		 		0);
		}
		else {
			Destroy(gameObject, 1.0f);
		}
	}
	
	void CalculateAndAddForceToPoint(Vector3 offset) {
		Vector3 worldPos = transform.TransformPoint(offset);
		GridPoint point = _elementManager.GridPointFromPosition(worldPos, true);
		
		if (point.x < 0 || point.y < 0) {
			return;
		}
		
		float height = _elementManager.CurrentTotalHeight[point.x][point.y];
		float waterHeight = _fluidLayer.HeightField[point.x][point.y];
		
		if (height > transform.position.y && waterHeight > 0.0001) {
			Velocity vel = _fluidLayer.VelocityField[point.x][point.y];
			Vector3 force = new Vector3(
				vel.u * _velocityToForceMultiplyer,
				0,
				vel.v * _velocityToForceMultiplyer);
			_rigidbody.AddForceAtPosition(force, worldPos);
		}
	}
	
}
