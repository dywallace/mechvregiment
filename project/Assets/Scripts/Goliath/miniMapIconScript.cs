﻿using UnityEngine;
using System.Collections;

public class miniMapIconScript : MonoBehaviour {

	public float life;
	public SpriteRenderer sprite;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(life >= 0){
			life -= Time.deltaTime;
		}

		if(life < 0){
			gameObject.SetActive(false);
		}

		Color tempColour =sprite.color ;
		tempColour.a = life/0.5f;
		sprite.color = tempColour;

	}

	void OnEnable(){
		life = 0.75f;
	}
}
