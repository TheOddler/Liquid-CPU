using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Spawner : MonoBehaviour {

	public ElementLayerManager _elementManager;
	public FluidLayer _fluidLayer;

	public Collider _colliderToAddOn;
	
	public List<FloatingObject> _floatingObjectPrefabs;
	
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
				
				var floatingPrefab = _floatingObjectPrefabs.ElementAtOrDefault(Random.Range(0, _floatingObjectPrefabs.Count));
				var obj = Instantiate(floatingPrefab.gameObject, pos, Quaternion.identity) as GameObject;
				var floater = obj.GetComponent<FloatingObject>();
				floater.Initialize(_elementManager, _fluidLayer);
			}
		}
	}
}
