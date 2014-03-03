using UnityEngine;
using System.Collections;

public class follow : MonoBehaviour {
    public GameObject target;
    public GameObject thisObject;
    public float height;
    public float offsetZ = 0;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        this.transform.position = new Vector3(target.transform.position.x, height, target.transform.position.z + offsetZ);
	}
}
