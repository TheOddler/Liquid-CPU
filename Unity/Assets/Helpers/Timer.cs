using UnityEngine;
using System.Collections;

public class Timer {

	int _frameCount = 0;
	float _best = float.MaxValue, _worst = float.MinValue, _average = 0, _last = 0;
	
	float _timeStart;
	float _timePartStart;
	float _totalTime;
	
	string _formatString = "0.0000";

	public void Start() {
		_timeStart = Time.realtimeSinceStartup;
		_totalTime = -1.0f;
	}

	public void Stop() {
		if (_totalTime < 0) { //parts wasn't used
			_totalTime = Time.realtimeSinceStartup - _timeStart;
		}
		
		_last = _totalTime;
		if (_totalTime < _best) _best = _totalTime;
		if (_totalTime > _worst) _worst = _totalTime;
		_average = ((_average * _frameCount) + _totalTime) / (++_frameCount); //!! ++_framecount
	}
	
	public void StartPart() {
		_timePartStart = Time.realtimeSinceStartup;
	}
	
	public void EndPart() {
		_totalTime += Time.realtimeSinceStartup - _timePartStart;
	}


	public override string ToString() {
		float bestms = _best*1000.0f;
		float worstms = _worst*1000.0f;
		float averagems = _average*1000.0f;
		float lastms = _last*1000.0f;
		
		return "Best: " + bestms.ToString(_formatString)
			+ "ms; Worst: " + worstms.ToString(_formatString)
			+ "ms; Average: " + averagems.ToString(_formatString)
			+ "ms; Last: " + lastms.ToString(_formatString);
	}
}
