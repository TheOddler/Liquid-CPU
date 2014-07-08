using UnityEngine;
using System;
using System.Collections;

public class Timer {

	const int AVARAGE_FRAME_COUNT = 60;

	int _frameCountForAvarage = 0;
	double _best = double.MaxValue, _worst = double.MinValue, _average = 0, _last = 0;
	double _totalForAvarage = 0;
	
	DateTime _timeStart;
	
	string _formatString = "00.0000";

	public void Start() {
		_timeStart = DateTime.UtcNow;//Time.realtimeSinceStartup;
	}

	public void Stop() {
		var deltaTime = (DateTime.UtcNow - _timeStart).TotalMilliseconds;		

		_last = deltaTime;
		if (deltaTime < _best) _best = deltaTime;
		if (deltaTime > _worst) _worst = deltaTime;
		
		_totalForAvarage += deltaTime;
		++_frameCountForAvarage;
		if (_frameCountForAvarage >= AVARAGE_FRAME_COUNT) {
			_average = _totalForAvarage / (double)_frameCountForAvarage;
			_frameCountForAvarage = 0;
			_totalForAvarage = 0;
		}
		
		//_average = ((_average * _frameCountForAvarage) + deltaTime) / (_frameCountForAvarage);
		//++_frameCountForAvarage;
	}
	
	
	public override string ToString() {
		return "Best: " + _best.ToString(_formatString)
			+ "ms; Worst: " + _worst.ToString(_formatString)
			+ "ms; Average: " + _average.ToString(_formatString)
			+ "ms; Last: " + _last.ToString(_formatString);
	}
}
