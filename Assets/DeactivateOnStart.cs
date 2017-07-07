using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeactivateOnStart : MonoBehaviour {

	// Use this for initialization
	void Start () {
        this.gameObject.SetActive(false);	
	}
}
