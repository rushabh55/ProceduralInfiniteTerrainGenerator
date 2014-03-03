using UnityEngine;
using System.Collections;
using System.Linq;

public class Mood : MonoBehaviour {
    public GameObject directionalLight;
    private Light _directionalLight;
    public GameObject _sandStorm;
    private System.Random rand = new System.Random(new System.Random().Next(0, 1000));
    public Material[] skyboxMaterials;
    public LensFlare sunMoon;
    public GameObject _snowFX;
	// Use this for initialization
	void Start () {
        _directionalLight = directionalLight.GetComponent<Light>();
        _directionalLight.intensity = (float)rand.NextDouble();

        if (rand.NextDouble() > 0.5)
        {
            if (rand.NextDouble() > 0.9)
            {
                _sandStorm.GetComponent<ParticleEmitter>().maxEmission = 0;
                _snowFX.GetComponent<ParticleEmitter>().maxEmission = 25000;
            }
            else
            {
                _sandStorm.GetComponent<ParticleEmitter>().maxEmission = 1000;
                _snowFX.GetComponent<ParticleEmitter>().maxEmission = 25000;
            }
        }
        else
        {
            _sandStorm.GetComponent<ParticleEmitter>().maxEmission = 250000;
            _snowFX.GetComponent<ParticleEmitter>().maxEmission = 2500;
        }
        var _skyMood = rand.Next(0, skyboxMaterials.Length);
        if(_skyMood <=5 )
        {
            
        }
        else
        {
            sunMoon.color = Color.white;
        }
        Debug.Log(_skyMood);
        RenderSettings.skybox = skyboxMaterials.ElementAt(_skyMood);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
