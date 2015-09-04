using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Ball : FloatingObject {
	
	public ElementLayerManager _elementManager;
	public FluidLayer _fluidLayer;
	
	public float _velocityToForceMultiplyer = 2.0f;
	public float _buoyancyMultiplyer = 50.0f;
	public float _minimumWaterForBuoyancy = 0.2f;
	public float _bottomOffset = -0.2f;

	public List<Material> _materials;
	
	Rigidbody _rigidbody;
	SphereCollider _collider;
	
	void Start() {
		_rigidbody = GetComponent<Rigidbody>();
		_collider = GetComponent<SphereCollider>();
		
		GetComponent<Renderer>().material = _materials.ElementAt(Random.Range(0, _materials.Count));
	}
	
	public override void Initialize(ElementLayerManager manager, FluidLayer fluid) {
		_elementManager = manager;
		_fluidLayer = fluid;
	}
	
	void FixedUpdate() {
		//Movement
		CalculateAndAddForceToPoint(new Vector3(0, 0, 0));
		
		//buoyancy
		GridPoint point = _elementManager.GridPointFromPosition(transform.position, true);
		if (point.x >= 0 && point.y >= 0) {
			float waterHeight = _fluidLayer.HeightField[point.x][point.y];
			
			float height = _elementManager.CurrentTotalHeight[point.x][point.y];
			float bottom = _collider.bounds.center.y + _bottomOffset;
			float buoyancy = (height - bottom) * _buoyancyMultiplyer * Mathf.Lerp(0, 1, waterHeight / _minimumWaterForBuoyancy);
			
		 	_rigidbody.AddForce(0, buoyancy, 0);
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
