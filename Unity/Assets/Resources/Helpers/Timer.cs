using UnityEngine;
using System.Collections;

public class Timer {

	int _frameCount = 0;
	float _best = float.MaxValue, _worst = float.MinValue, _average = 0;
	float _timeStart;

	public void Start() {
		_timeStart = Time.realtimeSinceStartup;
	}

	public void Stop() {
		float thisFrameTime = Time.realtimeSinceStartup - _timeStart;
		if (thisFrameTime < _best) _best = thisFrameTime;
		if (thisFrameTime > _worst) _worst = thisFrameTime;
		_average = ((_average * _frameCount) + thisFrameTime) / (++_frameCount); //!! ++_framecount
	}


	public override string ToString() {
		return "Best: " + _best*1000.0f + "ms; Worst: " + _worst*1000.0f + "ms; Average: " + _average*1000.0f + "ms";
	}
}
