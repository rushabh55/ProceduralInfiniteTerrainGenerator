using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;

public class move3X3_2 : MonoBehaviour {

    public float moveSpeed = 125;

    private List<terrainChunks> terrains = new List<terrainChunks>();
	public GameObject frontCube = null;
    public GameObject backCube = null;
	public Dimensions _dimension;

	public const int chunkSize = 9;

    private static Vector3 _terrainPosition;

    public int Twidth = 1024;
    public int Tlength = 1024;
    public int Theight = 75;

    private terrainChunks _trailingTerrain;

	// Use this for initialization
	void Start () {		
		generateTerrains();
		var _terrainCollection = GameObject.FindGameObjectsWithTag("Terrain");
        var playerPosStartTerrain = _terrainCollection.ElementAt(chunkSize / 2).GetComponent<Terrain>();
        this.transform.position = new Vector3(playerPosStartTerrain.transform.position.x, playerPosStartTerrain.transform.position.y - 100, playerPosStartTerrain.transform.position.z);

        int i = 0;
        int chunk = (int)Math.Sqrt(chunkSize);
        Terrain[] t = new Terrain[chunk];
        Terrain[] t2 = new Terrain[chunk];
        int j = 0;       
          
        while (i < _terrainCollection.Count())
        {
            t[j] = _terrainCollection[i].GetComponent<Terrain>();
            
            i++;
            j++;
            if (i % chunk == 0)
            {
                j = 0;
                terrainChunks TChunk = new terrainChunks(terrainChunkType.horizontal ,t[0], t[1], t[2]);
                terrains.Add(TChunk);
            }
        }

        ///Finding the 3x1 block of terrain that can be handy for rearrangements
        //w = 0; c * w = 0
        //w = 1; c * w = 3
        //w = 2; c * w = 6    
        for (int q = 0; q < chunk; q++)
        {
            for (int w = 0; w < chunk; w++)
            {
                t2[w] = _terrainCollection[q + chunk * w].GetComponent<Terrain>();
            }
            terrainChunks TChunk = new terrainChunks(terrainChunkType.vertical, t2[0], t2[1], t2[2]);
            terrains.Add(TChunk);
        }
        
        
	}

	void generateTerrains() {
        float x = 0, y = 0;
        float _terrainWidth = Twidth, _terrainLength = Tlength;
        for (int i = 0; i < (int)Math.Sqrt(9); i++)
        {
            for (int j = 0; j < (int)Math.Sqrt(9); j++)
            {
                x += _terrainWidth;
                _terrainPosition = new Vector3(x, 0, y);
                PerlinGenerate3X3();
            }
            x = 0;
            y += _terrainLength;
        }
	}

	// Update is called once per frame
	void Update () {
        Move();
        if (ReorderReqd())
        {
            terrainChunks c = findTerrainReorderChunks();
            if(c != null)
            {
                Reorder(c);
                _trailingTerrain = c;
            }
            else
            {
                Reorder(_trailingTerrain);
            }
            Debug.Log("Reordering");
        }
        return;
	}

    bool ReorderReqd()
    {
        var cube = frontCube;
        bool reqd = false;
        if (terrains.Count == 0)
        {
            Debug.Log("No Terrains");
            return reqd;
        }
        foreach (terrainChunks tChunk in terrains)
        {
            foreach (Terrain t in tChunk.terrainChunk)
            {
                Vector2 cubePos = new Vector2(cube.transform.position.x, cube.transform.position.z);
                Rect r = new Rect(t.transform.position.x, t.transform.position.z, t.terrainData.size.x, t.terrainData.size.z);
                if (r.Contains(cubePos))
                {
                    reqd = true;
                }
            }
        }
        return !reqd;
    }

    void Reorder(terrainChunks c)
    {
        if (c != null)
        {
            float x, y = 0;
            float _terrainWidth, _terrainLength;
            _terrainWidth = c.terrainChunk.FirstOrDefault().terrainData.size.x;
            _terrainLength = c.terrainChunk.FirstOrDefault().terrainData.size.z;
            if (c.type == terrainChunkType.horizontal)
            {
                x = frontCube.transform.position.x - _terrainWidth;
                y = frontCube.transform.position.z;

                for (int i = 0; i < (int)Math.Sqrt(chunkSize); i++)
                {
                    c.terrainChunk[i].transform.position = new Vector3(x, 0, y);
                    x += _terrainWidth;

                }
                //Debug.Log("Hori");
            }
            else
            {
                x = frontCube.transform.position.x;
                y = frontCube.transform.position.z - _terrainLength;

                for (int i = 0; i < (int)Math.Sqrt(chunkSize); i++)
                {
                    c.terrainChunk[i].transform.position = new Vector3(x, 0, y);
                    y += _terrainLength;
                }
                //Debug.Log("Vert");
            }
        }
    }

    terrainChunks findTerrainReorderChunks()
    {
        terrainChunks chunk = null;
        foreach (var c in terrains)
        {
            if (c.terrainChunk != null)
            {
                for (int i = 0; i < (int)Math.Sqrt(chunkSize); i++)
                {
                    Terrain t = c.terrainChunk[1];
                    Rect r = new Rect(t.transform.position.x, t.transform.position.z, t.terrainData.size.x, t.terrainData.size.z);
                    Vector2 pos = new Vector2(backCube.transform.position.x, backCube.transform.position.z);
                    if (r.Contains(pos))
                    {
                        return c;
                    }
                }
            }
        }
        return chunk;
    }

    void Move()
    {
        try
        {
            CharacterController controller = gameObject.GetComponent<CharacterController>();
            var x = Input.GetAxis("Horizontal");
            var y = Input.GetAxis("Vertical");
            Vector3 move = new Vector3(x, 0, y);
            move = transform.TransformDirection(move);
            move *= moveSpeed;
            controller.Move(move);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message.ToString());
        }
        return;
    }

    #region PerlinStuff


    public Texture2D texGrass = null;
    public Texture2D texDirt = null;
    float perlinFrequency = 1;
    float perlinAmplitude = 0.5f;
    int perlinOctaves = 8;
    float scale = 0.01f; // scale the indices so we end up with a reasonable heighmap based on them


    void PerlinGenerate3X3()
    {
    
    int CHUNKS = 1; // # of tiles of length and width, so 5 is a 5x5 grid

    Vector3 size = new Vector3(Twidth, Theight, Tlength); // middle number is terrain height
    GameObject[,] terrain = new GameObject[CHUNKS, CHUNKS];

    System.Random rand = new System.Random(42);
    TerrainData[] tData = new TerrainData[CHUNKS * CHUNKS];     
    float[][,] heightMap = new float[CHUNKS * CHUNKS][,];

    // the algorithm expects that all values are floating point numbers
    // and will return 0's when they aren't - so we budge
    // all indices by a consistent random number
    float xOffset = (float)rand.NextDouble();
    float yOffset = (float)rand.NextDouble();
    // scale
    const int ALPHA_TILE_SIZE = 2;
         for (int row = 0; row < CHUNKS; row++)
        {
            for (int col = 0; col < CHUNKS; col++)
            {
                tData[col * CHUNKS + row] = new TerrainData();
            }
        }
        Twidth = (int)size.x;
        Theight = (int)size.y;
        Tlength = (int)size.z;

        perlinCreateArrays(rand);
        for (int i = 0; i < CHUNKS * CHUNKS; i++)
        {
            heightMap[i] = new float[Twidth, Tlength];
        }

        int x = -(CHUNKS / 2);
        int y = -(CHUNKS / 2);
        for (int i = 0; i < CHUNKS; i++)
        {
            for (int j = 0; j < CHUNKS; j++)
            {
                // where is the terrain piece located?
                //whereIsTerrain[i, j] = new Vector3(x * (width - 1), 0, y * (length - 1));
                // create the perlin noise height map
                heightMap[i * CHUNKS + j] = k_perlin(heightMap[i * CHUNKS + j], Twidth, Tlength, 0.5f, 0, x * (Twidth - 1), y * (Tlength - 1));
                y++;

                // now make the height maps exactly align on their edges
                if (i != 0)
                {
                    for (int k = 0; k < Tlength; k++)
                    {
                        heightMap[i * CHUNKS + j][0, k] = heightMap[(i - 1) * CHUNKS + j][Tlength - 1, k];
                    }
                }

                if (j != 0)
                {
                    for (int k = 0; k < Tlength; k++)
                    {
                        heightMap[i * CHUNKS + j][k, 0] = heightMap[i * CHUNKS + (j - 1)][k, Tlength - 1];
                    }
                }
            }
            y = -(CHUNKS / 2);
            x++;
        }
        SplatPrototype[] test = new SplatPrototype[2];
        test[0] = new SplatPrototype();
        test[0].texture = texDirt;
        test[0].tileOffset = new Vector2(0, 0);
        test[0].tileSize = new Vector2(Twidth, Tlength);

        test[1] = new SplatPrototype();
        test[1].texture = texGrass;
        test[1].tileOffset = new Vector2(0, 0);
        test[1].tileSize = new Vector2(Twidth, Tlength);

        // NOTE: some of the indices for things are backwards because I had to figure
        // out the order to place them in for seamless terrain after I'd already written
        // the first code.
        float[, ,] singlePoint = new float[1, 1, test.Length];
        for (int row = 0; row < CHUNKS; row++)
        {
            for (int col = 0; col < CHUNKS; col++)
            {
                tData[col * CHUNKS + row].heightmapResolution = Twidth;
                tData[col * CHUNKS + row].alphamapResolution = ALPHA_TILE_SIZE * ALPHA_TILE_SIZE;
                tData[col * CHUNKS + row].SetDetailResolution(Twidth - 1, 16);
                tData[col * CHUNKS + row].baseMapResolution = Twidth - 1 + 1;
                tData[col * CHUNKS + row].SetHeights(0, 0, heightMap[col * CHUNKS + row]);
                tData[col * CHUNKS + row].size = new Vector3(Twidth - 1, Theight, Tlength - 1);
                tData[col * CHUNKS + row].splatPrototypes = test;

                // set the actual textures in each tile here.
                for (int i = 0; i < ALPHA_TILE_SIZE; i++)
                {
                    for (int j = 0; j < ALPHA_TILE_SIZE; j++)
                    {

                        if (heightMap[col * CHUNKS + row][i * Twidth / ALPHA_TILE_SIZE, j * Tlength / ALPHA_TILE_SIZE] > 0.5)
                        {
                            singlePoint[0, 0, 0] = 0f;
                            singlePoint[0, 0, 1] = 1f;
                        }
                        else
                        {
                            singlePoint[0, 0, 0] = 1f;
                            singlePoint[0, 0, 1] = 0f;
                        }

                        // this is amazingly stupid, but alpha is only able to be at every point
                        // and not altogether as far as I can tell.
                        tData[col * CHUNKS + row].SetAlphamaps(j, i, singlePoint);
                    }
                }

                terrain[row, col] = Terrain.CreateTerrainGameObject(tData[col * CHUNKS + row]);
                terrain[row, col].transform.position = _terrainPosition;
                terrain[row, col].gameObject.tag = "Terrain";
                terrain[row, col].gameObject.isStatic = false;
            }
        }
    }
    const int PERM_SIZE = 256;
    const int DIMENSIONS = 3;
    public static float[,] randomArray = new float[PERM_SIZE + PERM_SIZE + 2, 3];
    public static int[] permutation = new int[PERM_SIZE + PERM_SIZE + 2];


    // creating the random permutation and noise arrays
    // a step that many of the perlin alg's seem to lack
    void perlinCreateArrays(System.Random rand)
    {
        float s;
        Vector3 tmp;
        float[] v = new float[DIMENSIONS];
        int i;
        int j;
        int k;

        // create an array of random gradient vectors uniformly on the unit sphere
        for (i = 0; i < PERM_SIZE; i++)
        {
            do
            {
                for (j = 0; j < DIMENSIONS; j++)
                {
                    v[j] = (float)((rand.Next() % (PERM_SIZE + PERM_SIZE)) - PERM_SIZE) / PERM_SIZE;
                }
                tmp = new Vector3(v[0], v[1], v[2]);
                s = Vector3.Dot(tmp, tmp);
            } while (s > 1.0);

            s = Mathf.Sqrt(s);
            for (j = 0; j < DIMENSIONS; j++)
            {
                randomArray[i, j] = v[j] / s;
            }


        }

        // create a pseudorandom permutation of [1 .. PERM_SIZE]
        for (i = 0; i < PERM_SIZE; i++)
        {
            permutation[i] = i;
        }

        for (i = PERM_SIZE; i > 0; i -= 2)
        {
            permutation[i] = i;
            k = permutation[i];
            permutation[i] = permutation[j = rand.Next() % PERM_SIZE];
            permutation[j] = k;
        }

        // extend arrays to allow for faster indexing
        for (i = 0; i < PERM_SIZE + 2; i++)
        {
            permutation[PERM_SIZE + i] = permutation[i];
            for (j = 0; j < DIMENSIONS; j++)
            {
                randomArray[PERM_SIZE + i, j] = randomArray[i, j];
            }
        }

    }

    // utility function that interpolates data from different points on the surface
    private static float lerpP(float t, float a, float b)
    {
        return a + t * (b - a);
    }

    // a utility function that sets up variables used by the noise function from Perlin's paper
    private void setup(int i, float[] vec, out int b0, out int b1, out float r0, out float r1, out float t)
    {
        t = vec[i] + 10000.0f;
        b0 = ((int)t) & (PERM_SIZE - 1);
        b1 = (b0 + 1) & (PERM_SIZE - 1);
        r0 = t - (int)t;
        r1 = r0 - 1;
    }

    // a utility function in Perlin's paper 
    // gives the cubic approximation of the component dropoff
    private float s_curve(float t)
    {
        return (t * t * (3.0f - 2.0f * t));

    }

    // A dot product between two vectors represented in a bunch of individual float var's
    // from Perlin's paper
    private float dotProduct(float q1, float q2, float q3, float r1, float r2, float r3)
    {
        Vector3 tmp2 = new Vector3(q1, q2, q3);
        Vector3 tmp = new Vector3(r1, r2, r3);
        return Vector3.Dot(tmp2, tmp);
    }

    // utility function for a different dropoff function that can be tried
    private float fade(float t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }

    // the Perlin noise algorithm as written by Perlin
    // lots of dot products and lerps
    private float pnoise(float x, float y, float z)
    {

        int bx0, bx1, by0, by1, bz0, bz1, b00, b10, b01, b11;
        float rx0, rx1, ry0, ry1, rz0, rz1, sx, sy, sz, a, b, c, d, t, u, v;
        int i, j;

        float[] vec = new float[3]; vec[0] = x; vec[1] = y; vec[2] = z;

        setup(0, vec, out bx0, out bx1, out rx0, out rx1, out t);
        setup(1, vec, out by0, out by1, out ry0, out ry1, out t);
        setup(2, vec, out bz0, out bz1, out rz0, out rz1, out t);

        i = permutation[bx0];
        j = permutation[bx1];

        b00 = permutation[i + by0];
        b10 = permutation[j + by0];
        b01 = permutation[i + by1];
        b11 = permutation[j + by1];

        sx = s_curve(rx0);
        sy = s_curve(ry0);
        sz = s_curve(rz0);

        // This uses a different dropoff function that's supposed to work better.
        // uncomment to see the difference
        //sx = fade(rx0);  
        //sy = fade(ry0); 
        //sz = fade(rz0);

        u = dotProduct(randomArray[b00 + bz0, 0], randomArray[b00 + bz0, 1], randomArray[b00 + bz0, 2],
            rx0, ry0, rz0);
        v = dotProduct(randomArray[b10 + bz0, 0], randomArray[b10 + bz0, 1], randomArray[b10 + bz0, 2],
            rx1, ry0, rz0);
        a = lerpP(sx, u, v);

        u = dotProduct(randomArray[b01 + bz0, 0], randomArray[b01 + bz0, 1], randomArray[b01 + bz0, 2],
            rx0, ry1, rz0);
        v = dotProduct(randomArray[b11 + bz0, 0], randomArray[b11 + bz0, 1], randomArray[b11 + bz0, 2],
            rx1, ry1, rz0);
        b = lerpP(sx, u, v);

        c = lerpP(sy, a, b);

        u = dotProduct(randomArray[b00 + bz1, 0], randomArray[b00 + bz1, 1], randomArray[b00 + bz1, 2],
            rx0, ry0, rz1);
        v = dotProduct(randomArray[b10 + bz1, 0], randomArray[b10 + bz1, 1], randomArray[b10 + bz1, 2],
            rx1, ry0, rz1);
        a = lerpP(sx, u, v);

        u = dotProduct(randomArray[b01 + bz1, 0], randomArray[b01 + bz1, 1], randomArray[b01 + bz1, 2],
            rx0, ry1, rz1);
        v = dotProduct(randomArray[b11 + bz1, 0], randomArray[b11 + bz1, 1], randomArray[b11 + bz1, 2],
            rx1, ry1, rz1);
        b = lerpP(sx, u, v);

        d = lerpP(sy, a, b);

        return (1.5f * lerpP(sz, c, d));
    }

    // return a single value for a heightmap
    // apply gain as discussed by Perlin
    private float height2d(float x, float y, int octaves,
             float lacunarity = 1.0f, float gain = 1.0f)
    {

        float freq = perlinFrequency, amp = perlinAmplitude;
        float sum = 0.0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += pnoise(x * freq, y * freq, 0) * amp;
            freq *= lacunarity; // amount we increase freq by for each loop through
            amp *= gain;
        }

        return sum;
    }

    // fill a 2D array with perlin noise which represents a heightmap
    private float[,] k_perlin(float[,] pos, int width, int height,
            float gain, float zOffset, float positionX, float positionZ)
    {

        // since we want the noise to be consistent based on the indices
        // of the map, we scale and offset them
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                pos[i, j] = height2d((positionX + i) * scale + 0, (positionZ + j) * scale + 0, perlinOctaves, 2.0f, gain) + zOffset;
            }
        }


        pos = normalizePerlin(pos, new Vector2(width, height));

        return pos;
    }

    // Normalize all data
    private float[,] normalizePerlin(float[,] heightMap, Vector2 arraySize)
    {
        int Tx = (int)arraySize.x;
        int Ty = (int)arraySize.y;
        int Mx;
        int My;
        float highestPoint = 1.0f;
        float lowestPoint = -1f;

        // Normalise...
        float heightRange = highestPoint - lowestPoint;
        float normalisedHeightRange = 1.0f;
        float normaliseMin = 0.0f;
        for (My = 0; My < Ty; My++)
        {
            for (Mx = 0; Mx < Tx; Mx++)
            {
                float normalisedHeight = ((heightMap[Mx, My] - lowestPoint) / heightRange) * normalisedHeightRange;
                heightMap[Mx, My] = normaliseMin + normalisedHeight;
            }
        }

        // return the height map
        return heightMap;
    }


    public void GeneratePerlinNoiseAtPoint(Vector2 position)
    {
        //terrainPosition = position;
        Start();
    }

    #endregion
}

class terrainChunks
{
    public Terrain[] terrainChunk;
    public terrainChunkType type;
    public terrainChunks(terrainChunkType type, params Terrain[] terrains)
    {
        terrainChunk = terrains;
    }
}

public struct Dimensions
{
	public double height;
    public double width;
}

public enum terrainChunkType
{
    horizontal,
    vertical
}