using UnityEngine;
using System.Collections;

public abstract class FloatingObject : MonoBehaviour {
	
	public abstract void Initialize(ElementLayerManager manager, FluidLayer fluid);
	
}
