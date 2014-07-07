using UnityEngine;
using System;
using System.Collections;

public class Timer {

	int _frameCount = 0;
	double _best = double.MaxValue, _worst = double.MinValue, _average = 0, _last = 0;
	
	DateTime _timeStart;
	
	string _formatString = "0.0000";

	public void Start() {
		_timeStart = DateTime.UtcNow;//Time.realtimeSinceStartup;
	}

	public void Stop() {
		var deltaTime = (DateTime.UtcNow - _timeStart).TotalMilliseconds;		

		_last = deltaTime;
		if (deltaTime < _best) _best = deltaTime;
		if (deltaTime > _worst) _worst = deltaTime;
		_average = ((_average * _frameCount) + deltaTime) / (++_frameCount); //!! ++_framecount
	}
	
	
	public override string ToString() {
		return "Best: " + _best.ToString(_formatString)
			+ "ms; Worst: " + _worst.ToString(_formatString)
			+ "ms; Average: " + _average.ToString(_formatString)
			+ "ms; Last: " + _last.ToString(_formatString);
	}
}
