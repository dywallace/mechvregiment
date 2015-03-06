﻿using UnityEngine;
using System.Collections;

public class MuzzleFlashScript : MonoBehaviour {

	float life = 0.11f;
	PoolManager pool;
	// Use this for initialization
	void Start () {
		pool = transform.parent.GetComponent<PoolManager>();
	}
	
	// Update is called once per frame
	void Update () {

		life -= Time.deltaTime;

		if (life <= 0) {
			pool.Deactivate(gameObject);
		}
	}

	void OnEnable(){
		life = 0.11f;
	}

}