﻿/**
 * 
 * Tracks player attributes and handles behaviours
 * 
 **/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour {

	public int id = -1;

	public float HealWait = 5.0f;
	public float MaxHealth = 100;
	private float InvMaxHealth = 1;
	public float RegenInc = 25f;
	public float RespawnWait = 5.0f;
	public float StunLength = 3.0f;
	private float stunProg = 0;
	public float AmmoReplenishTime = 2;
	private float ammoReplenishProg = 0;

	// Inputs
	public Camera playerCam;
	public ControllerScript playerController;
	public PlayerNetSend networkManager;
	public ScavUI display;
	public LayerMask shootableLayer;
	public LayerMask groundedLayer;
	public Animator anim;
	public Animator fpsAnim;
	public ScavLayer initializer;
	public ScavGame game;

	// Status variables
	private float health = 0;
	private float healTimer = 0;
	[HideInInspector]
	public float respawnTimer = 0;
	private bool isAimingDownSights = false;
	public bool isDead = false;

	public GameObject weaponWrapper;
	public GameObject weaponWrapper3;
	private GameObject[] weaponModels;
	private GameObject[] weaponModels3;
	private Weapon[] weapons;
	public GameObject crystal;
	public GameObject crystalTP;

	[HideInInspector]
	public int currentWeaponIndex = 0;
	[HideInInspector]
	public bool isStunned = false;
	[HideInInspector]
	public bool isReplenishingAmmo = false;
	[HideInInspector]
	public bool readyToEnd = false;
	[HideInInspector]
	public Rigidbody rigidbody;

	// Recorded variables
	private Vector3 startingPos;
	private Animator deathCamAnim;
	private int flinchHash = Animator.StringToHash("Flinch");
	private int resetHash = Animator.StringToHash("Reset");
	private int fwdDeadHash = Animator.StringToHash("DieFwd");
	private int bckDeadHash = Animator.StringToHash("DieBck");

	public SplitAudioListener splitListener;
	public AudioSource deathSound;

	// Use this for initialization
	void Start () {
		InvMaxHealth = 1 / MaxHealth;
		health = MaxHealth;
		startingPos = transform.position;
		deathCamAnim = display.deathCam.GetComponent<Animator>();
		rigidbody = GetComponent<Rigidbody> ();
		splitListener.StoreAudioSource (deathSound);
	}
	
	// Update is called once per frame
	void Update () {
		if (!isDead){
			TryRegen();

			// Weapon ammunition restoration
			if (ammoReplenishProg > 0){
				ammoReplenishProg -= Time.deltaTime;
			}
			if (isReplenishingAmmo && ammoReplenishProg <= 0){
				ammoReplenishProg = AmmoReplenishTime;
				foreach(Weapon weapon in weapons){
					weapon.AttemptPartialReplenish();
				}
			}
		}
		else{
			TryRespawn();
		}
		display.UpdateCrosshairSpread(weapons [currentWeaponIndex].GetSpread ());

		if (stunProg > 0){
			stunProg -= Time.deltaTime;
			if (stunProg <= 0){
				isStunned = false;
			}
		}
	}

	public void Initialize(int playerId, float[] window, float uiScale){
		id = playerId;

		int weaponCount = weaponWrapper.transform.childCount;
		weaponModels = new GameObject[weaponCount];
		for(int i = 0; i < weaponCount; i++){
			weaponModels[i] = weaponWrapper.transform.GetChild(i).gameObject;
		}

		int weaponCount3 = weaponWrapper3.transform.childCount;
		weaponModels3 = new GameObject[weaponCount3];
		for(int i = 0; i < weaponCount3; i++){
			weaponModels3[i] = weaponWrapper3.transform.GetChild(i).gameObject;
		}

		weapons = new Weapon[weaponCount];

		// Storing weapon references to component scripts
		for (int i = 0; i < weaponCount; i++) {
			weapons[i] = weaponModels[i].GetComponent<Weapon>();
			weapons[i].SetPlayerReference(this);
			weapons[i].SetControllerReference(this.playerController);
			weapons[i].gameObject.SetActive(false);

			game.splitListener.StoreAudioSource (weapons[i].gunshotSound);
			game.splitListener.StoreAudioSource (weapons[i].reloadSound);
		}

		weapons [currentWeaponIndex].gameObject.SetActive (true);

		// Setting player UI
		display.Initialize(window[0], window[1], window[2], window[3], uiScale, weapons[currentWeaponIndex].GetSpread());

		// Setting controller
		playerController.SetController(id);
	}

	public void SetToKeyboard(){
		playerController.isKeyboard = true;
	}

	private void TryRespawn(){
		if (respawnTimer > 0){
			respawnTimer -= Time.deltaTime;
		}
		else{
			Respawn();
		}
	}

	private void Respawn(){
		isDead = false;
		transform.position = startingPos;
		health = MaxHealth;
		healTimer = 0;
		display.UpdateDamageOverlay (0);

		playerController.ResetWeaponSelected();

		// Enable firing layer
		anim.SetLayerWeight(1, 1);
		anim.SetTrigger(resetHash);
		fpsAnim.SetTrigger(resetHash);

		networkManager.photonView.RPC ("PlayerRespawn", PhotonTargets.All, initializer.Layer - 1, transform.position);

		foreach (Weapon weapon in weapons){
			if (weapon){
				weapon.ReplenishWeapon();
			}
		}

		weapons [currentWeaponIndex].gameObject.SetActive (false);
		weapons [currentWeaponIndex].gameObject.SetActive (true);

		display.EndRespawnSequence ();
		GetComponent<Rigidbody>().isKinematic = false;
	}

	// Regenerates if healing timer is depleted and health is below maximum
	private void TryRegen(){
		if (health < MaxHealth){
			if (healTimer > 0){
				healTimer -= Time.deltaTime;
			}
			else{
				Regen();
			}
		}
	}

	private void Regen(){
		health = Mathf.Min(MaxHealth, health + RegenInc * Time.deltaTime);
		display.UpdateDamageOverlay (1 - health * InvMaxHealth);
	}

	public bool ToggleADS(bool? setADS = null){
		if (setADS != null) {
			isAimingDownSights = (bool)setADS;
			weapons[currentWeaponIndex].SetAds(isAimingDownSights);
		}
		else{
			isAimingDownSights = !isAimingDownSights;
			weapons[currentWeaponIndex].SetAds(isAimingDownSights);
		}

		return isAimingDownSights;
	}

	// Changes currently selected weapon
	public void CycleWeapons(int adjustment){
		int prevWeaponIndex = currentWeaponIndex;
		currentWeaponIndex = GetExpectedWeaponIndex(adjustment);

		// Activate new weapon
		if (prevWeaponIndex != currentWeaponIndex){
			weapons [prevWeaponIndex].StopReloading();
			weapons [prevWeaponIndex].gameObject.SetActive (false);
			weaponModels3[prevWeaponIndex].SetActive(false);
			weapons [currentWeaponIndex].gameObject.SetActive (true);
			weaponModels3 [currentWeaponIndex].SetActive(true);
			display.ActivateNewWeapon(currentWeaponIndex);
		}
		networkManager.photonView.RPC ("PlayerCycleWeapon", PhotonTargets.All, initializer.Layer - 1, currentWeaponIndex);
	}

	public void FlagRetrieved(){
		weapons[currentWeaponIndex].gameObject.SetActive(false);
		weaponModels3[currentWeaponIndex].SetActive(false);
		crystal.SetActive(true);
		crystalTP.SetActive (true);

		game.FlagRetrieved(gameObject);
		display.UpdateObjective(game.exitPoint);
		game.exitPoint.SetActive(true);
		display.dropFlagPrompt.SetActive(true);
		display.grabFlagPrompt.SetActive(false);

		networkManager.photonView.RPC ("ScavengerPickedUpFlag", PhotonTargets.All, initializer.Layer - 1);
	}

	public void FlagDropped(){
		weapons[currentWeaponIndex].gameObject.SetActive(true);
		weaponModels3[currentWeaponIndex].SetActive(true);
		crystal.SetActive(false);
		crystalTP.SetActive (false);
		display.dropFlagPrompt.SetActive(false);

		game.FlagDropped(transform.position);
		game.exitPoint.SetActive(false);

		networkManager.photonView.RPC ("ScavengerDroppedFlag", PhotonTargets.All, transform.position + Vector3.up, initializer.Layer - 1);
	}
	
	// Deals damage to player and resets healing timer
	public void Damage(float damage, Vector3 direction){
		if (!isDead){
			health -= damage;
			healTimer = HealWait;

			health = Mathf.Max (health, 0);

			//print (initializer.Layer + " " + damage + ":" + health);

			display.IndicateDamageDirection (direction);
			display.UpdateDamageOverlay (1 - health * InvMaxHealth);

			if (health <= 0){
				Kill (direction);
			}

			anim.SetTrigger(flinchHash);
		}
	}

	private void Kill(Vector3 direction){
		isDead = true;
		isReplenishingAmmo = false;
		respawnTimer = RespawnWait;
		if (playerController.flagPickedUp){
			playerController.DropFlag();
		}

		splitListener.PlayAudioSource (deathSound);
		
		// Disable firing layer
		anim.SetLayerWeight(1, 0);
		
		display.StartRespawnSequence(RespawnWait);
		
		if (Vector3.Angle(direction, transform.forward) < 90){
			anim.SetTrigger(fwdDeadHash);
			fpsAnim.SetTrigger(fwdDeadHash);
			deathCamAnim.SetTrigger(fwdDeadHash);
			networkManager.photonView.RPC ("PlayerDeath", PhotonTargets.All, initializer.Layer - 1, true);
		}
		else{
			anim.SetTrigger(bckDeadHash);
			fpsAnim.SetTrigger(bckDeadHash);
			deathCamAnim.SetTrigger(bckDeadHash);
			networkManager.photonView.RPC ("PlayerDeath", PhotonTargets.All, initializer.Layer - 1, false);
		}
		
		if (playerController.IsGrounded()){
			GetComponent<Rigidbody>().isKinematic = true;
		}
	}

	public Weapon GetCurrentWeapon(){
		return weapons [currentWeaponIndex];
	}

	// Attempts to fire bullet
	public void SetFiringState(bool isFiring){
		weapons [currentWeaponIndex].setFiringState (isFiring);
	}

	public void TriggerReload(){
		weapons [currentWeaponIndex].TryReloading ();
	}

	public void TriggerHitMarker(){
		display.TriggerHitMarker();
	}

	public int GetExpectedWeaponIndex(int adjustment){
		int newWeaponIndex = currentWeaponIndex;
		newWeaponIndex += adjustment;
		if (newWeaponIndex >= weapons.Length){
			newWeaponIndex = 0;
		}
		else if (newWeaponIndex < 0){
			newWeaponIndex = weapons.Length - 1;
		}

		return newWeaponIndex;
	}

	public void Launch(){
		if (!isStunned){
			Damage(game.goliath.DashDamage, game.goliath.botJoint.transform.forward);
			GetComponent<Rigidbody>().velocity = 10 * (game.goliath.botJoint.transform.forward + new Vector3(Random.Range(game.goliath.botJoint.transform.right.x, -game.goliath.botJoint.transform.right.x), 0, Random.Range(game.goliath.botJoint.transform.right.z, -game.goliath.botJoint.transform.right.z)) + Vector3.up * 1.5f);
			stunProg = StunLength;
			isStunned = true;
		}
	}
}
