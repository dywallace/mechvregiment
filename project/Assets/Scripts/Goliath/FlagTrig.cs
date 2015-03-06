﻿using UnityEngine;
using System.Collections;

public class FlagTrig : MonoBehaviour {

	public MechShoot mechShooty;
	public bool flagActive;
	// Use this for initialization
	void Start () {
		flagActive = false;
	}
	
	// Update is called once per frame
	void Update () {
		if(flagActive == true && SixenseInput.Controllers[1].GetButtonDown(SixenseButtons.BUMPER)){
			pickedUp();
		}


	}

	void OnTriggerEnter(Collider collide){
		print(collide.tag);
		if(collide.tag == "Player"){
			flagActive = true;
		}
		//display Right bumper to pick up UI
	}

	void pickedUp(){
		mechShooty.carrying = true;
		flagActive = false;
	}
}