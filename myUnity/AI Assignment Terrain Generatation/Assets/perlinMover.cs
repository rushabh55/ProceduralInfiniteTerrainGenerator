using UnityEngine;
using System.Collections;
using System;

public class perlinMover : MonoBehaviour
{
    #region Declarations
    public Material[] skyboxMaterials = null;
    public static int width;
    public static int length;
    public static int height;
    public const int CHUNKS = 3; // # of tiles of length and width, so 5 is a 5x5 grid

    public Vector3 size = new Vector3(width, height, length); // middle number is terrain height
    private static GameObject[,] terrain = new GameObject[CHUNKS, CHUNKS];
    private Vector3[,] whereIsTerrain = new Vector3[CHUNKS, CHUNKS];
    private static System.Random rand = new System.Random(1);
    private TerrainData[] tData = new TerrainData[CHUNKS * CHUNKS];
    private float[][,] heightMap = new float[CHUNKS * CHUNKS][,];

    // variables for perlin noise generation

    // perlin noise variables
    public float perlinFrequency = 1;
    public float perlinAmplitude = 0.5f;
    public int perlinOctaves = 8;
    public float perlinLacunarity = 0.5f;
    public float perlinGain = 1.0f;
    // the algorithm expects that all values are floating point numbers
    // and will return 0's when they aren't - so we budge
    // all indices by a consistent random number
    private float xOffset = (float)rand.NextDouble();
    private float yOffset = (float)rand.NextDouble();

    // scale
    private float scale = 0.01f; // scale the indices so we end up with a reasonable heighmap based on them
    private const int ALPHA_TILE_SIZE = 64;
    private bool middleMove = true;

    SplatPrototype[] test = new SplatPrototype[7];
    //0 GoodDIrt
    //1 Grass
    //2 Grass&Pock
    //3 Cliff
    //4 snow1
    //5 snow2
    //6 snow3

    public GameObject _treeInstance = null;

    public TreePrototype[] tree = null;    
    public int voronoiCells = 15;
    public float voronoiFeatures;
    public float voronoiScale { get; set; }
    public bool moved { get; set; }
    #endregion

    #region start and Update
    // Use this for initialization
    void Start()
    {
        width = (int)size.x;
        length = (int)size.z;
        height = (int)size.y;
//        Debug.Log(width);
        RenderSettings.skybox = skyboxMaterials[rand.Next(0, skyboxMaterials.Length)];

        test[0] = new SplatPrototype();
        test[0].texture = (Texture2D)Resources.Load("GoodDirt", typeof(Texture2D));
        test[0].tileOffset = new Vector2(0, 0);
        test[0].tileSize = new Vector2(width, length);

        test[1] = new SplatPrototype();
        test[1].texture = (Texture2D)Resources.Load("Grass", typeof(Texture2D));
        test[1].tileOffset = new Vector2(0, 0);
        test[1].tileSize = new Vector2(width, length);

        test[2] = new SplatPrototype();
        test[2].texture = (Texture2D)Resources.Load("Grass&Rock", typeof(Texture2D));
        test[2].tileOffset = new Vector2(0, 0);
        test[2].tileSize = new Vector2(width, length);

        test[3] = new SplatPrototype();
        test[3].texture = (Texture2D)Resources.Load("Cliff", typeof(Texture2D));
        test[3].tileOffset = new Vector2(0, 0);
        test[3].tileSize = new Vector2(width, length);

        test[4] = new SplatPrototype();
        test[4].texture = (Texture2D)Resources.Load("Snow/1", typeof(Texture2D));
        test[4].tileOffset = new Vector2(0, 0);
        test[4].tileSize = new Vector2(width, length);

        test[5] = new SplatPrototype();
        test[5].texture = (Texture2D)Resources.Load("Snow/3", typeof(Texture2D));
        test[5].tileOffset = new Vector2(0, 0);
        test[5].tileSize = new Vector2(width, length);

        test[6] = new SplatPrototype();
        test[6].texture = (Texture2D)Resources.Load("Snow/2", typeof(Texture2D));
        test[6].tileOffset = new Vector2(0, 0);
        test[6].tileSize = new Vector2(width, length);
        generate();
        this.transform.position = new Vector3(terrain[CHUNKS / 2, CHUNKS / 2].transform.position.x, height, terrain[CHUNKS / 2, CHUNKS / 2].transform.position.z) + new Vector3(width / 2, 0, length / 2);
        GenerateTrees();
    }

    // Update is called once per frame
    void Update()
    {
        this.Move();
        var TILE_SIZE = length;
        if (this.transform.position.x / TILE_SIZE - _prevPos.x / TILE_SIZE > 1 && !moved)
        {
            MovePosX();
            moved = true;
        }

        if (this.transform.position.x / TILE_SIZE - _prevPos.x / TILE_SIZE < -1 && !moved)
        {
            MoveNegX();
            moved = true;
        }

        if (this.transform.position.z / TILE_SIZE - _prevPos.z / TILE_SIZE < -1 && !moved)
        {
            MoveNegZ();
            moved = true;
        }

        if (this.transform.position.z / TILE_SIZE - _prevPos.z / TILE_SIZE > 1 && !moved)
        {
            MovePosZ();
            moved = true;
        }

        if (moved)
        {
            if (middleMove)
            {
                middleMove = false;
                CalcNoise(terrain[CHUNKS / 2,CHUNKS / 2].GetComponent<Terrain>());
            }
            //  foreach(var t in terrain)
            moved = !moved;
            _prevPos = this.transform.position;
        }

        //ForcedReorderNeeded();
    }
	#endregion

    #region FORCEREORDER
    public GameObject frontCube = null;
    bool ForcedReorderNeeded()
    {
        bool flag = false;
        if (frontCube == null)
        {
            throw new Exception();
        }
        foreach (var t in terrain)
        {
            Vector2 fCubePos = new Vector2(frontCube.transform.position.x, frontCube.transform.position.z);
			var td = t.GetComponent<Terrain>();
            Rect r = new Rect(t.transform.position.x, t.transform.position.z, td.terrainData.size.x, td.terrainData.size.z);
            if (!r.Contains(frontCube.transform.position))
            {
                flag = true;
            }
        }

        if (flag)
        {
            Debug.Log("FORCE REORDERED");
            var TILE_SIZE = length;
            if (this.transform.position.x / TILE_SIZE - _prevPos.x / TILE_SIZE > 0 && !moved)
            {
                MovePosX();
                moved = true;
            }

            if (this.transform.position.x / TILE_SIZE - _prevPos.x / TILE_SIZE < 0 && !moved)
            {
                MoveNegX();
                moved = true;
            }

            if (this.transform.position.z / TILE_SIZE - _prevPos.z / TILE_SIZE < 0 && !moved)
            {
                MoveNegZ();
                moved = true;
            }

            if (this.transform.position.z / TILE_SIZE - _prevPos.z / TILE_SIZE > 0 && !moved)
            {
                MovePosZ();
                moved = true;
            }
        }

        return flag;
    }
    #endregion

    #region TerrainMOVE

    private static Vector3 _prevPos;
    private void MovePosZ()
    {
        if (terrain == null)
            return;

        GameObject[,] pos = new GameObject[CHUNKS, CHUNKS];

        pos = terrain;
        for (int i = 0; i < CHUNKS; i++)
        {
            pos[i, 0].transform.position = terrain[i, 1].transform.position;
            pos[i, 1].transform.position = terrain[i, 2].transform.position;
            pos[i, 2].transform.position = terrain[i, 2].transform.position + new Vector3(0, 0, length - 4);
        }

        for (int i = 0; i < CHUNKS; i++)
        {
            CalcNoise(pos[i, 2].GetComponent<Terrain>());
        }

        var main = pos[0, 2].GetComponent<Terrain>();
        pos[0, 2].GetComponent<Terrain>().SetNeighbors(null, null, pos[1, 2].GetComponent<Terrain>(), pos[0, 1].GetComponent<Terrain>());
        pos[1, 2].GetComponent<Terrain>().SetNeighbors(pos[0, 2].GetComponent<Terrain>(), null, pos[2, 2].GetComponent<Terrain>(), pos[1, 1].GetComponent<Terrain>());
        pos[2, 2].GetComponent<Terrain>().SetNeighbors(pos[1, 2].GetComponent<Terrain>(), null, null, pos[2, 1].GetComponent<Terrain>());
        m_zOff += length;
        terrain = pos;
    }

    private void MoveNegZ()
    {
        if (terrain == null)
            return;
        GameObject[,] pos = new GameObject[CHUNKS, CHUNKS];
        pos = terrain;
        for (int i = 0; i < CHUNKS; i++)
        {
            //terrain[0, i].transform.position = terrain[2, i].transform.position + new Vector3(width, 0, 0);
            pos[i, 2].transform.position = terrain[i, 1].transform.position;
            pos[i, 1].transform.position = terrain[i, 0].transform.position;
            pos[i, 0].transform.position = terrain[i, 0].transform.position - new Vector3(0, 0, length - 4);
        }

        for (int i = 0; i < CHUNKS; i++)
        {
            CalcNoise(pos[i, 0].GetComponent<Terrain>());
        }
        terrain = pos;
    }

    private void MoveNegX()
    {
        if (terrain == null)
            return;
        GameObject[,] pos = new GameObject[CHUNKS, CHUNKS];
        pos = terrain;
        for (int i = 0; i < CHUNKS; i++)
        {
            //terrain[0, i].transform.position = terrain[2, i].transform.position + new Vector3(width, 0, 0);
            pos[2, i].transform.position = terrain[1, i].transform.position;
            pos[1, i].transform.position = terrain[0, i].transform.position;
            pos[0, i].transform.position = terrain[0, i].transform.position - new Vector3(width - 4, 0, 0);
        }
        for (int i = 0; i < CHUNKS; i++)
        {
            CalcNoise(pos[0, i].GetComponent<Terrain>());
        }
        terrain = pos;
    }

    Vector2 moveVec = new Vector2();

    void MovePosX()
    {
        if (terrain == null)
            return;
        GameObject[,] pos = new GameObject[CHUNKS, CHUNKS];
        pos = terrain;
        for (int i = 0; i < CHUNKS; i++)
        {
            //terrain[0, i].transform.position = terrain[2, i].transform.position + new Vector3(width, 0, 0);
            pos[0, i].transform.position = terrain[1, i].transform.position;
            pos[1, i].transform.position = terrain[2, i].transform.position;
            pos[2, i].transform.position = terrain[2, i].transform.position + new Vector3(width - 4, 0, 0);
        } 
        for (int i = 0; i < CHUNKS; i++)
        {
            CalcNoise(pos[2, i].GetComponent<Terrain>());
        }
        terrain = pos;
    }
    #endregion

    #region Char Control
    public float moveSpeed;
    public void Move()
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
        catch (System.Exception e)
        {
            Debug.Log(e.Message.ToString());
        }
        return;
    }
    #endregion

    #region TerrainGen
    void generate()
    {
        width = (int)size.x;
        height = (int)size.y;
        length = (int)size.z;

        perlinCreateArrays(rand);
        for (int i = 0; i < CHUNKS * CHUNKS; i++)
        {
            heightMap[i] = new float[width, length];

        }

        int x = -(CHUNKS / 2);
        int y = -(CHUNKS / 2);


        for (int i = 0; i < CHUNKS * CHUNKS; i++)
        {
            heightMap[i] = generateVoronoi(heightMap[i], new Vector2(width - 1, length - 1), false);
        }

        for (int i = 0; i < CHUNKS; i++)
        {
            for (int j = 0; j < CHUNKS; j++)
            {
                whereIsTerrain[i, j] = new Vector3(x * (width - 1), 0, y * (length - 1));
                heightMap[i * CHUNKS + j] = k_perlin(heightMap[i * CHUNKS + j], width, length, 0.5f, 0, x * (width - 1), y * (length - 1), false);
                m_xOff += width;
                m_zOff += length;
                y++;
                if (i != 0)
                {
                    for (int k = 0; k < length - 1; k++)
                    {
                        heightMap[i * CHUNKS + j][0, k] = heightMap[(i - 1) * CHUNKS + j][length - 1, k];
                    }
                }
                if (j != 0)
                {
                    for (int k = 0; k < length - 1; k++)
                    {
                        heightMap[i * CHUNKS + j][k, 0] = heightMap[i * CHUNKS + (j - 1)][k, length - 1];
                    }
                }
            }
            y = -(CHUNKS / 2);
            x++;
        }

        float[, ,] singlePoint = new float[1, 1, test.Length];
        for (int row = 0; row < CHUNKS; row++)
        {
            for (int col = 0; col < CHUNKS; col++)
            {
                tData[col * CHUNKS + row] = new TerrainData();
                tData[col * CHUNKS + row].heightmapResolution = width;
                tData[col * CHUNKS + row].alphamapResolution = ALPHA_TILE_SIZE;
                tData[col * CHUNKS + row].SetDetailResolution(width - 1, 8);
                tData[col * CHUNKS + row].baseMapResolution = width - 1 + 1;
                tData[col * CHUNKS + row].SetHeights(0, 0, heightMap[col * CHUNKS + row]);
                tData[col * CHUNKS + row].size = new Vector3(width - 1, height, length - 1);
                tData[col * CHUNKS + row].splatPrototypes = test;

                DetailPrototype[] dProtos = new DetailPrototype[2];
                dProtos[0] = new DetailPrototype();
                dProtos[0].prototype = water;
                dProtos[0].healthyColor = Color.white;
                dProtos[0].dryColor = Color.black;
                dProtos[0].noiseSpread = 1f;
                dProtos[0].maxHeight = 1;
                dProtos[0].maxWidth = 1;
                dProtos[0].renderMode = DetailRenderMode.VertexLit;
                dProtos[0].usePrototypeMesh = true;
                
                dProtos[1] = new DetailPrototype();
                dProtos[1].prototypeTexture = grass;
                dProtos[1].healthyColor = Color.white;
                dProtos[1].dryColor = Color.black;
                dProtos[1].renderMode = DetailRenderMode.Grass;
                dProtos[1].noiseSpread = 1f;
                dProtos[1].maxWidth = 1;
                dProtos[1].maxHeight = 1;
                dProtos[1].minHeight = 0.5f;
                dProtos[1].minWidth = 0.5f;
                dProtos[1].usePrototypeMesh = false;
                dProtos[1].bendFactor = 1f;

                tData[col * CHUNKS + row].detailPrototypes = dProtos;


                TreePrototype[] _treeprotos = new TreePrototype[1];
                _treeprotos[0] = new TreePrototype();
                _treeprotos[0].prefab = _treeInstance;
                _treeprotos[0].bendFactor = 1f;
                _treeprotos[0].prefab.renderer.material.color = Color.white;

                tData[col * CHUNKS + row].treePrototypes = _treeprotos;

                for (int i = 0; i < ALPHA_TILE_SIZE; i++)
                {
                    for (int j = 0; j < ALPHA_TILE_SIZE; j++)
                    {
                        //Texture INDEX.
                        //0 GoodDIrt
                        //1 Grass
                        //2 Grass&Pock
                        //3 Cliff
                        //4 snow1
                        //5 snow2
                        //6 snow3
                        //...end..
                        var CurrentHeight = heightMap[col * CHUNKS + row][i * width / ALPHA_TILE_SIZE, j * length / ALPHA_TILE_SIZE];
                        var blendFactor = (float)rand.NextDouble();
#region TEXTURING
                        if (CurrentHeight < 0.2f && CurrentHeight > 0.0f)
                        {                            
                            singlePoint[0, 0, 0] = blendFactor / 2f;
                            singlePoint[0, 0, 1] = blendFactor / 2f;
                            singlePoint[0, 0, 2] = 1f - blendFactor;
                            singlePoint[0, 0, 3] = 0f;
                            singlePoint[0, 0, 4] = 0f;
                            singlePoint[0, 0, 5] = 0f;
                            singlePoint[0, 0, 6] = 0f;
                        }
                        else
                            if (CurrentHeight < 0.3)
                            {
                                singlePoint[0, 0, 0] = 0f;
                                singlePoint[0, 0, 1] = 0f;
                                singlePoint[0, 0, 2] = 1f;
                                singlePoint[0, 0, 3] = 0f;
                                singlePoint[0, 0, 4] = 0f;
                                singlePoint[0, 0, 5] = 0f;
                                singlePoint[0, 0, 6] = 0f;
                            }
                            else
                                if (CurrentHeight < 0.6)
                                {
                                    singlePoint[0, 0, 0] = 0f;
                                    singlePoint[0, 0, 1] = blendFactor;
                                    singlePoint[0, 0, 2] = 0f;
                                    singlePoint[0, 0, 3] = 1f - blendFactor;
                                    singlePoint[0, 0, 4] = 0f;
                                    singlePoint[0, 0, 5] = 0f;
                                    singlePoint[0, 0, 6] = 0f;
                                }
                                else
                                    if (CurrentHeight < 0.8)
                                    {
                                        singlePoint[0, 0, 0] = 0f;
                                        singlePoint[0, 0, 1] = 0f;
                                        singlePoint[0, 0, 2] = 0f;
                                        singlePoint[0, 0, 3] = 0.1f + blendFactor;
                                        singlePoint[0, 0, 4] = 1f;
                                        singlePoint[0, 0, 5] = 0f;
                                        singlePoint[0, 0, 6] = 0f;
                                    }
                                    else
                                    {
                                        singlePoint[0, 0, 0] = 0f;
                                        singlePoint[0, 0, 1] = 0f;
                                        singlePoint[0, 0, 2] = 0f;
                                        singlePoint[0, 0, 3] = 0f;
                                        singlePoint[0, 0, 4] = 0f;
                                        singlePoint[0, 0, 5] = 0f;
                                        singlePoint[0, 0, 6] = 1f;
                                    }
                        tData[col * CHUNKS + row].SetAlphamaps(j, i, singlePoint);
                        #endregion
                    }
                }
                terrain[row, col] = Terrain.CreateTerrainGameObject(tData[col * CHUNKS + row]);
                terrain[row, col].transform.position = whereIsTerrain[row, col] - new Vector3(2.87f * row, 0, 2.87f * col);
                terrain[row, col].name = (++terrno).ToString();                
            }
        }
//        Debug.Log(averageHeight);
        averageHeight /= (height * width);
        averageHeight /= CHUNKS;
       // Debug.Log(averageHeight);
    }
    int terrno = 0;
    public int noOfTreesPerTerrain = 0;
    #endregion

	#region ADDING TREES
    public GameObject water;
    public Texture2D grass;
	void GenerateTrees()
	{
        foreach (var terr in terrain)
        {
            var t = terr.GetComponent<Terrain>();

            var totalTrees = noOfTreesPerTerrain;
            totalTrees += rand.Next(0, 30);
            for (int i = 0; i < totalTrees; i++)
            {
                Vector2 r = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble());
                var ht = t.terrainData.GetHeight((int)r.x, (int)r.y);
                var h1 = 0.1 * height;
                var h2 = 0.4 * height;
//                Debug.Log(r);
//                Debug.Log(ht);
                if (ht > h1 && ht < h2)
                {
                    TreeInstance ti = new TreeInstance();
                    ti.position = new Vector3(r.x, 0, r.y);
                    ti.lightmapColor = Color.white;
                    ti.color = Color.white;
                    ti.heightScale = 1 + (float)rand.NextDouble();
                    ti.widthScale = 1 + (float)rand.NextDouble();
                    terr.GetComponent<Terrain>().AddTreeInstance(ti);
                    Debug.Log(r);
                }
                else
                {
                    //i--;
                }
                terr.GetComponent<Terrain>().Flush();
                terr.GetComponent<Terrain>().terrainData.RefreshPrototypes();
            }
        }
	}

    void GenerateTreesAtPoint(int x, int y, Terrain t, params object[] otherParams)
    {
        var r = new Vector2(x, y);
        TreeInstance ti = new TreeInstance();
        ti.position = new Vector3(r.x, 0, r.y);
        ti.lightmapColor = Color.white;
        ti.color = Color.white;
        ti.heightScale = (float)rand.NextDouble();
        ti.widthScale = (float)rand.NextDouble();
        t.AddTreeInstance(ti);
    }
	#endregion

	#region Perlin
	const int PERM_SIZE = 256;
	const int DIMENSIONS = 3;
	public static float[,] randomArray = new float[PERM_SIZE + PERM_SIZE + 2, 3];
	public static int[] permutation = new int[PERM_SIZE + PERM_SIZE + 2];
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
            float gain, float zOffset, float positionX, float positionZ, bool regen)
    {

        // since we want the noise to be consistent based on the indices
        // of the map, we scale and offset them
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //Debug.Log(i);
                //Debug.Log(j);
                var w = height2d((positionX + i) * scale + xOffset, (positionZ + j) * scale + yOffset, perlinOctaves, 2.0f, gain) + zOffset;
                if (regen)
                    pos[i, j] = w;
                else
                    pos[i, j] += w;
            }
        }


        pos = normalizePerlin(ref pos, new Vector2(width, height));

        return pos;
    }

    decimal averageHeight = 0;
    // Normalize all data
    private float[,] normalizePerlin(ref float[,] heightMap, Vector2 arraySize)
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
                heightMap[Mx, My] = normaliseMin + (float)normalisedHeight;
                averageHeight += (double)heightMap[Mx, My] == double.NaN ? 0 : (decimal)heightMap[Mx, My];
            }
        }

        // return the height map
        return heightMap;
    }

    int x, y;
    #endregion

    #region Veronoi
    public struct Peak
    {
        public Vector2 peakPoint;
        public float peakHeight;
    }

    public class PeakDistance : IComparable
    {
        public int id;
        public float dist;

        public int CompareTo(object obj)
        {
            PeakDistance Compare = (PeakDistance)obj;
            int result = this.dist.CompareTo(Compare.dist);
            if (result == 0)
            {
                result = this.dist.CompareTo(Compare.dist);
            }
            return result;
        }
    }

    public enum VoronoiType { Linear = 0, Sine = 1, Tangent = 2 };
    public VoronoiType voronoiType;
    //		voronoiPresets.Add(new voronoiPresetData("Scattered Peaks", VoronoiType.Linear, 16, 8, 0.5f, 1.0f));
    //	voronoiPresets.Add(new voronoiPresetData("Rolling Hills", VoronoiType.Sine, 8, 8, 0.0f, 1.0f));
    //	voronoiPresets.Add(new voronoiPresetData("Jagged Mountains", VoronoiType.Linear, 32, 32, 0.5f, 1.0f));
    private float[,] generateVoronoi(float[,] heightMap, Vector2 arraySize, bool regenFlag)
    {
        int Tx = (int)arraySize.x;
        int Ty = (int)arraySize.y;
        // Create Voronoi set...
        ArrayList voronoiSet = new ArrayList();
        int i;
        int inc = 1;
        if (regenFlag)
            inc++;
        for (i = 0; i < voronoiCells; i+=inc)
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
        for (My = 0; My < Ty; My+=inc)
        {
            for (Mx = 0; Mx < Tx; Mx+=inc)
            {
                ArrayList peakDistances = new ArrayList();
                try
                {
                    for (i = 0; i < voronoiCells; i += inc)
                    {
                        Peak peakI = (Peak)voronoiSet[i];
                        Vector2 peakPoint = peakI.peakPoint;
                        float distanceToPeak = Vector2.Distance(peakPoint, new Vector2(Mx, My));
                        PeakDistance newPeakDistance = new PeakDistance();
                        newPeakDistance.id = i;
                        newPeakDistance.dist = distanceToPeak;
                        peakDistances.Add(newPeakDistance);
                    }
                }
                catch (Exception) { }
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

    #endregion

    #region Recalculate Noise
    int m_xOff, m_zOff;
    void CalcNoise(Terrain t)
    {
        voronoiCells = 8;
        var heightMap = t.terrainData.GetHeights(0, 0, (int)width, (int)length);     
        heightMap = k_perlin(heightMap, width, length, 0.95f, 0, 0, 0, true);
        xOffset += width;
        NewTextures(t, heightMap, width, height);
        t.terrainData.RefreshPrototypes();
        t.terrainData.SetHeights(0, 0, heightMap);
    }

    private void NewTextures(Terrain t, float[,] heightMap, int width, int height)
    {
        for (int i = 0; i < width - 1; i++)
        {
            for (int j = 0; j < width - 1; j++)
            {
                #region RE - TEXTURING
                var CurrentHeight = heightMap[i, j];
                float[, ,] singlePoint = new float[1,1,7];
                var blendFactor = (float)rand.NextDouble();
                if (CurrentHeight < 0.2f && CurrentHeight > 0.0f)
                {
                    singlePoint[0, 0, 0] = blendFactor / 2f;
                    singlePoint[0, 0, 1] = blendFactor / 2f;
                    singlePoint[0, 0, 2] = 1f - blendFactor;
                    singlePoint[0, 0, 3] = 0f;
                    singlePoint[0, 0, 4] = 0f;
                    singlePoint[0, 0, 5] = 0f;
                    singlePoint[0, 0, 6] = 0f;
                }
                else
                    if (CurrentHeight < 0.3)
                    {
                        singlePoint[0, 0, 0] = 0f;
                        singlePoint[0, 0, 1] = 0f;
                        singlePoint[0, 0, 2] = 1f;
                        singlePoint[0, 0, 3] = 0f;
                        singlePoint[0, 0, 4] = 0f;
                        singlePoint[0, 0, 5] = 0f;
                        singlePoint[0, 0, 6] = 0f;
                    //    GenerateTreesAtPoint(i, j, t, null);
                    }
                    else
                        if (CurrentHeight < 0.6)
                        {
                            singlePoint[0, 0, 0] = 0f;
                            singlePoint[0, 0, 1] = blendFactor;
                            singlePoint[0, 0, 2] = 0f;
                            singlePoint[0, 0, 3] = 1f - blendFactor;
                            singlePoint[0, 0, 4] = 0f;
                            singlePoint[0, 0, 5] = 0f;
                            singlePoint[0, 0, 6] = 0f;
                        }
                        else
                            if (CurrentHeight < 0.8)
                            {
                                singlePoint[0, 0, 0] = 0f;
                                singlePoint[0, 0, 1] = 0f;
                                singlePoint[0, 0, 2] = 0f;
                                singlePoint[0, 0, 3] = 0.1f + blendFactor;
                                singlePoint[0, 0, 4] = 1f;
                                singlePoint[0, 0, 5] = 0f;
                                singlePoint[0, 0, 6] = 0f;
                            }
                            else
                            {
                                singlePoint[0, 0, 0] = 0f;
                                singlePoint[0, 0, 1] = 0f;
                                singlePoint[0, 0, 2] = 0f;
                                singlePoint[0, 0, 3] = 0f;
                                singlePoint[0, 0, 4] = 0f;
                                singlePoint[0, 0, 5] = 0f;
                                singlePoint[0, 0, 6] = 1f;
                            }
                t.terrainData.SetAlphamaps(j, i, singlePoint);
                #endregion
            }
        }
    }
    #endregion

}
