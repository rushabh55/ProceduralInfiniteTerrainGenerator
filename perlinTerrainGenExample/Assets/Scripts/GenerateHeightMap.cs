using UnityEngine;
using System.Collections;
using System;

public class GenerateHeightMap : MonoBehaviour
{
	public static int width = 129;
	public static int length = 129;
	public static int height = 30;
	public const int CHUNKS = 1;
	public GameObject[,] terrain = new GameObject[CHUNKS,CHUNKS];
	public Vector3[,] whereIsTerrain = new Vector3[CHUNKS,CHUNKS];
	public Material material;
	public Vector3 size = new Vector3(width, height, length);
	private System.Random rand = new System.Random(42);
	private float[,] heightMap = new float[width,length];
	private const int ALPHA_TILE_SIZE = 128;
	public enum VoronoiType {Linear = 0, Sine = 1, Tangent = 2};
    public int voronoiTypeInt = 0;
    public VoronoiType voronoiType = VoronoiType.Linear;
    public int voronoiCells = 16;
    public float voronoiFeatures = 1.0f;
    public float voronoiScale = 1.0f;
    public float voronoiBlend = 1.0f;

	
	// Use this for initialization
	void Start ()
	{
		width = (int)size.x;
		height = (int)size.y;
		length = (int)size.z;

		// fill the height map with random numbers
		for (int i=0; i<width; i++) {
			for (int j=0;j<length; j++) {
				heightMap[i,j] = (float)rand.NextDouble();
			}
		}
		
		// check out a voronoi pattern
		Vector2 heightMapSize = new Vector2(width, length);
        heightMap = generateVoronoi(heightMap, heightMapSize);

		// calculate where terrain is
		int x = - (CHUNKS/2);
		int y = - (CHUNKS/2);
		for (int i=0;i<CHUNKS;i++) {
			for (int j=0;j<CHUNKS;j++){
				whereIsTerrain[i,j] = new Vector3(x * width, 0, y * length);
				y++;
			}
			y = - (CHUNKS/2);
			x++;
		}

		// create initial terrain pieces and place them
		// assume character is always in the center tile		
		TerrainData tData = new TerrainData();
		tData.heightmapResolution = width;
		tData.alphamapResolution = ALPHA_TILE_SIZE;
		tData.SetDetailResolution(width-1,16);
		tData.baseMapResolution = width;
		tData.SetHeights(0,0,heightMap);
		tData.size = new Vector3(width-1, height, length-1);

		// put a few different textures on them
		// note: this will need some work
		SplatPrototype[] test = new SplatPrototype[3];
     	test[0] = new SplatPrototype(); 
		test[0].texture = (Texture2D)Resources.Load("GoodDirt",typeof(Texture2D));
		test[0].tileOffset = new Vector2(0, 0); 
		test[0].tileSize = new Vector2(128, 128);
		
		test[1] = new SplatPrototype(); 
		test[1].texture = (Texture2D)Resources.Load("Grassy",typeof(Texture2D));
		test[1].tileOffset = new Vector2(0, 0); 
		test[1].tileSize = new Vector2(128, 128);

		test[2] = new SplatPrototype(); 
		test[2].texture = (Texture2D)Resources.Load("snow",typeof(Texture2D));
		test[2].tileOffset = new Vector2(0, 0); 
		test[2].tileSize = new Vector2(128, 128);

    	tData.splatPrototypes = test;
		
    	float[, ,] alphamaps = new float[128, 128, test.Length];
     	float[, ,] singlePoint = new float[1, 1, test.Length];
		
    	// set the actual textures in each tile here.
		for (int i=0;i<ALPHA_TILE_SIZE;i++) {
			for (int j=0;j<ALPHA_TILE_SIZE;j++){
				
				if (heightMap[i,j*length/ALPHA_TILE_SIZE] > 0.5) {
					alphamaps[i,j,0] = 0;
					alphamaps[i,j,1] = 0;
					alphamaps[i,j,2] = 1;
					singlePoint = new float[1, 1, test.Length];
					singlePoint[0,0,0] = 0f;
					singlePoint[0,0,1] = 0f;
					singlePoint[0,0,2] = 1f;
					
				} else if (heightMap[i,j*length/ALPHA_TILE_SIZE] > 0.0) {
					alphamaps[i,j,0] = 0.0f;
					alphamaps[i,j,1] = 1.0f;
					alphamaps[i,j,2] = 0;
					singlePoint = new float[1, 1, test.Length];
					singlePoint[0,0,0] = 0f;
					singlePoint[0,0,1] = 1f;
					singlePoint[0,0,2] = 0f;

				} else {
					alphamaps[i,j,0] = 1.0f;
					alphamaps[i,j,1] = 0.0f;
					alphamaps[i,j,2] = 0;
					singlePoint = new float[1, 1, test.Length];
					singlePoint[0,0,0] = 1f;
					singlePoint[0,0,1] = 0f;
					singlePoint[0,0,2] = 0f;

				}

				// this is amazingly stupid, but alpha is only able to be at every point
				// and not altogether as far as I can tell.
				tData.SetAlphamaps(j, i, singlePoint);
			}
		}

		// set up the terrain chunks with the default tile
		for (int i=0; i<CHUNKS; i++) {
			for (int j=0; j<CHUNKS; j++) {
				terrain[i,j] = Terrain.CreateTerrainGameObject(tData);
				terrain[i,j].transform.position = whereIsTerrain[i,j];
			}
		}
		
	}
	
	// Update is called once per frame
	void Update ()
	{	
	}

	
	public struct Peak {
		public Vector2 peakPoint;
		public float peakHeight;
	}
	
	public class PeakDistance : IComparable {
		public int id;
		public float dist;
	
		public int CompareTo(object obj) {
			PeakDistance Compare = (PeakDistance) obj;
			int result = this.dist.CompareTo(Compare.dist);
			if (result == 0) {
				result = this.dist.CompareTo(Compare.dist);
			}
			return result;
		}
	}
	
    //		voronoiPresets.Add(new voronoiPresetData("Scattered Peaks", VoronoiType.Linear, 16, 8, 0.5f, 1.0f));
	//	voronoiPresets.Add(new voronoiPresetData("Rolling Hills", VoronoiType.Sine, 8, 8, 0.0f, 1.0f));
	//	voronoiPresets.Add(new voronoiPresetData("Jagged Mountains", VoronoiType.Linear, 32, 32, 0.5f, 1.0f));
    private float[,] generateVoronoi(float[,] heightMap, Vector2 arraySize)
    {
        int Tx = (int)arraySize.x;
        int Ty = (int)arraySize.y;
        // Create Voronoi set...
        ArrayList voronoiSet = new ArrayList();
        int i;
        for (i = 0; i < voronoiCells; i++)
        {
            Peak newPeak = new Peak();
            int xCoord = (int)Mathf.Floor(UnityEngine.Random.value * Tx);
            int yCoord = (int)Mathf.Floor(UnityEngine.Random.value * Ty);
            float pointHeight = UnityEngine.Random.value;
            if (UnityEngine.Random.value > voronoiFeatures)
            {
                pointHeight = 0.0f;
            }
            newPeak.peakPoint = new Vector2(xCoord, yCoord);
            newPeak.peakHeight = pointHeight;
            voronoiSet.Add(newPeak);
        }
        int Mx;
        int My;
        float highestScore = 0.0f;
        for (My = 0; My < Ty; My++)
        {
            for (Mx = 0; Mx < Tx; Mx++)
            {
                ArrayList peakDistances = new ArrayList();
                for (i = 0; i < voronoiCells; i++)
                {
                    Peak peakI = (Peak)voronoiSet[i];
                    Vector2 peakPoint = peakI.peakPoint;
                    float distanceToPeak = Vector2.Distance(peakPoint, new Vector2(Mx, My));
                    PeakDistance newPeakDistance = new PeakDistance();
                    newPeakDistance.id = i;
                    newPeakDistance.dist = distanceToPeak;
                    peakDistances.Add(newPeakDistance);
                }
                peakDistances.Sort();
                PeakDistance peakDistOne = (PeakDistance)peakDistances[0];
                PeakDistance peakDistTwo = (PeakDistance)peakDistances[1];
                int p1 = peakDistOne.id;
                float d1 = peakDistOne.dist;
                float d2 = peakDistTwo.dist;
                float scale = Mathf.Abs(d1 - d2) / ((Tx + Ty) / Mathf.Sqrt(voronoiCells));
                Peak peakOne = (Peak)voronoiSet[p1];
                float h1 = (float)peakOne.peakHeight;
                float hScore = h1 - Mathf.Abs(d1 / d2) * h1;
                float asRadians;
                switch (voronoiType)
                {
                    case VoronoiType.Linear:
                        // Nothing...
                        break;
                    case VoronoiType.Sine:
                        asRadians = hScore * Mathf.PI - Mathf.PI / 2;
                        hScore = 0.5f + Mathf.Sin(asRadians) / 2;
                        break;
                    case VoronoiType.Tangent:
                        asRadians = hScore * Mathf.PI / 2;
                        hScore = 0.5f + Mathf.Tan(asRadians) / 2;
                        break;
                }
                hScore = (hScore * scale * voronoiScale) + (hScore * (1.0f - voronoiScale));
                if (hScore < 0.0f)
                {
                    hScore = 0.0f;
                }
                else if (hScore > 1.0f)
                {
                    hScore = 1.0f;
                }
                heightMap[Mx, My] = hScore;
                if (hScore > highestScore)
                {
                    highestScore = hScore;
                }
            }

        }

        // Normalise...
        for (My = 0; My < Ty; My++)
        {
            for (Mx = 0; Mx < Tx; Mx++)
            {
                float normalisedHeight = heightMap[Mx, My] * (1.0f / highestScore);
                heightMap[Mx, My] = normalisedHeight;
            }
        }
        return heightMap;
    }

} // end of class