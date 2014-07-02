using UnityEngine;
using System.Collections;

public abstract class ElementLayer: MonoBehaviour {
	
	public abstract float[][] Height { get; }
	public abstract void AddSource(float[][] source);
	public abstract void DoUpdate(float dt, float dx, float[][] lowerLayersHeight);
	public abstract void ApplyVisuals(float[][] lowerLayersHeight);
	
}
