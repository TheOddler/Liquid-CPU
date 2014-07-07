using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour {

	public ElementLayerManager _elementManager;
	public FluidLayer _fluidLayer;

	public Collider _colliderToAddOn;
	public Boat _boatPrefab;
	
	public float _spawnHeightOffset = 0.3f;
	
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonDown(2)) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			if (_colliderToAddOn.Raycast(ray, out hit, float.PositiveInfinity)) {
				GridPoint gridPoint = _elementManager.GridPointFromPosition(hit.point, true);
				
				Vector3 pos = hit.point;
				pos.y = _elementManager.CurrentTotalHeight[gridPoint.x][gridPoint.y] + _spawnHeightOffset;
				
				var obj = Instantiate(_boatPrefab.gameObject, pos, Quaternion.identity) as GameObject;
				var boat = obj.GetComponent<Boat>();
				boat.Initialize(_elementManager, _fluidLayer);
			}
		}
	}
}
