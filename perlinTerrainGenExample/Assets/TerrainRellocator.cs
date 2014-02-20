using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class TerrainRellocator : MonoBehaviour {
    public int height;
    public int width;
	public GameObject player;
    private Queue<PerlinNoise> terrains = new Queue<PerlinNoise>();
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (terrains.Count <= 1)
        {
            PerlinNoise noise = new PerlinNoise();
            noise.GeneratePerlinNoiseAtPoint(new Vector2(this.transform.position.x, this.transform.position.y));
            terrains.Enqueue(noise);
        }
		foreach(var terrain in terrains)
		{
			Rect r1 = new Rect(terrain.transform.position.x, terrain.transform.position.y, width, height);
			if(r1.Contains(new Vector2(player.transform.position.x, player.transform.position.y)))
			{
				var t = player.transform.localToWorldMatrix;
				Debug.Log (t);
			}
		}
	}	
}


class Midpoint
{
    public static Vector3 getMidPoint(ref Vector3 point)
    {
        Vector3 p = Vector3.zero;
        p.x = point.x + PerlinNoise.width / 2;
        p.y = point.y + PerlinNoise.height / 2;
        p.z = point.z;
        return p;
    }
}