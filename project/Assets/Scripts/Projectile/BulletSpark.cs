﻿using UnityEngine;
using System.Collections;

public class BulletSpark : MonoBehaviour {

	PoolManager pool;

	// Use this for initialization
	void Start () {
		pool = transform.parent.GetComponent<PoolManager>();
	}
	
	// Update is called once per frame
	void Update () {
		if (GetComponent<ParticleEmitter>().particleCount == 0){
			pool.Deactivate(gameObject);
		}
	}

	void OnEnable(){
		GetComponent<ParticleEmitter>().Emit ();
	}
}
