using UnityEngine;
using System.Collections;
using System;
using System.Threading;

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
    private float zOffset = (float)rand.NextDouble();

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

    public GameObject[] _treeInstance = null;

    public TreePrototype[] tree = null;

    public Texture2D[] detailObjects = null;

    public int voronoiCells = 15;
    public float voronoiFeatures;

    public float voronoiScale { get; set; }
    public bool moved { get; set; }
    #endregion

    #region start and Update
    // Use this for initialization
    void Start()
    {
        try
        {
            Application.targetFrameRate = 120;
            width = (int)size.x;
            length = (int)size.z;
            height = (int)size.y;
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
            m_xOff = width * CHUNKS;
            m_zOff = length * CHUNKS;
            //RescaleNormalize();
            NormalizeEverything();
        }
        catch (Exception)
        {
        }
    }

    void NormalizeEverything()
    {
        var terrain1 = terrain[0, 0].GetComponent<Terrain>();
        var terrain2 = terrain[0, 1].GetComponent<Terrain>();
        var terrain3 = terrain[0, 2].GetComponent<Terrain>();
        var terrain4 = terrain[1, 0].GetComponent<Terrain>();
        var terrain5 = terrain[1, 1].GetComponent<Terrain>();
        var terrain6 = terrain[1, 2].GetComponent<Terrain>();
        var terrain7 = terrain[2, 0].GetComponent<Terrain>();
        var terrain8 = terrain[2, 1].GetComponent<Terrain>();
        var terrain9 = terrain[2, 2].GetComponent<Terrain>();

        //terrain1.SetNeighbors( LEFT     TOP     RIGHT   BOTTOM)
        terrain1.SetNeighbors(null, terrain2, terrain4, null);
        terrain2.SetNeighbors(null, terrain3, terrain5, terrain1);
        terrain3.SetNeighbors(null, null, terrain6, terrain2);
        terrain4.SetNeighbors(terrain1, terrain5, terrain7, null);
        terrain5.SetNeighbors(terrain2, terrain6, terrain8, terrain4);
        terrain6.SetNeighbors(terrain3, null, terrain9, terrain5);
        terrain7.SetNeighbors(terrain4, terrain8, null, null);
        terrain8.SetNeighbors(terrain5, terrain9, null, terrain9);
        terrain9.SetNeighbors(terrain6, null, null, terrain8);
        try
        {
            stichAll(ref terrain1, null, terrain2, terrain4, null);
            stichAll(ref terrain2, null, terrain3, terrain5, terrain1);
            stichAll(ref terrain3, null, null, terrain6, terrain2);
            stichAll(ref terrain4, terrain1, terrain5, terrain7, null);
            stichAll(ref terrain5, terrain2, terrain6, terrain8, terrain4);
            stichAll(ref terrain6, terrain3, null, terrain9, terrain5);
            stichAll(ref terrain7, terrain4, terrain8, null, null);
            stichAll(ref terrain8, terrain5, terrain9, null, terrain9);
            stichAll(ref terrain9, terrain6, null, null, terrain8);
        }
        catch (Exception _e)
        {
        }
        terrain1.Flush();
        terrain2.Flush();
        terrain3.Flush();
        terrain4.Flush();
        terrain5.Flush();
        terrain6.Flush();
        terrain7.Flush();
        terrain8.Flush();
        terrain9.Flush();

        terrain1.terrainData.RefreshPrototypes();
        terrain2.terrainData.RefreshPrototypes();
        terrain3.terrainData.RefreshPrototypes();
        terrain4.terrainData.RefreshPrototypes();
        terrain5.terrainData.RefreshPrototypes();
        terrain6.terrainData.RefreshPrototypes();
        terrain7.terrainData.RefreshPrototypes();
        terrain8.terrainData.RefreshPrototypes();
        terrain9.terrainData.RefreshPrototypes();
    }

    void NormalizeBoundaries()
    {
        //var ht1 = new float[1, 64];
        //var ht2 = new float[64, 1];

        //foreach (var t in terrain)
        //{
        //    t.GetComponent<Terrain>().terrainData.SetHeights(0, 63, ht1);
        //    t.GetComponent<Terrain>().terrainData.SetHeights(63, 0, ht2);
        //}
    }

    void Update()
    {
        try
        {
            this.Move();
            var TILE_SIZE = length;
            if (this.transform.position.x / TILE_SIZE - _prevPos.x / TILE_SIZE > 1 && !moved)
            {
                m_xOff += width;
                MovePosX();
                moved = true;

                _prevPos.x = this.transform.position.x;
            }

            if (this.transform.position.x / TILE_SIZE - _prevPos.x / TILE_SIZE < -1 && !moved)
            {
                m_xOff *= -1;
                m_xOff -= width;
                MoveNegX();
                m_xOff *= -1;
                moved = true;

                _prevPos.x = this.transform.position.x;
            }

            if (this.transform.position.z / TILE_SIZE - _prevPos.z / TILE_SIZE < -1 && !moved)
            {
                m_zOff -= length;
                m_zOff *= -1;
                MoveNegZ();
                m_zOff *= -1;
                moved = true;
                _prevPos.z = this.transform.position.z;
            }

            if (this.transform.position.z / TILE_SIZE - _prevPos.z / TILE_SIZE > 1 && !moved)
            {
                m_zOff += length;
                MovePosZ();
                moved = true;
                _prevPos.z = this.transform.position.z;
            }

            if (moved)
            {
                NormalizeEverything();
                moved = !moved;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
                moveSpeed += .5f;
            if (Input.GetKeyUp(KeyCode.LeftShift))
                moveSpeed -= .5f;

            //ForcedReorderNeeded();
        }
        catch (Exception)
        {
        }
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

        Debug.Log("Move Pos z");
        GameObject[,] newTerrains = new GameObject[CHUNKS, CHUNKS];

        for (int i = 0; i < CHUNKS; i++)
        {
            terrain[i, 0].transform.position = terrain[i, 2].transform.position + new Vector3(0, 0, length - 2);
            CalcNoise(terrain[i, 0].GetComponent<Terrain>());
        }

        for (int i = 0; i < CHUNKS; i++)
        {
            for (int j = 0; j < CHUNKS; j++)
            {
                if (j == 2)
                    newTerrains[i, j] = terrain[i, 0];
                else
                {
                    newTerrains[i, j] = terrain[i, j + 1];
                }
            }
        }
        terrain = newTerrains;
    }

    private void MoveNegZ()
    {
        if (terrain == null)
            return;

        Debug.Log("Move Pos z");
        GameObject[,] newTerrains = new GameObject[CHUNKS, CHUNKS];

        for (int i = 0; i < CHUNKS; i++)
        {
            terrain[i, 2].transform.position = terrain[i, 0].transform.position - new Vector3(0, 0, length - 2);
            CalcNoise(terrain[i, 2].GetComponent<Terrain>());
        }

        for (int i = 0; i < CHUNKS; i++)
        {
            for (int j = 0; j < CHUNKS; j++)
            {
                if (j == 0)
                    newTerrains[i, j] = terrain[i, 2];
                else
                {
                    newTerrains[i, j] = terrain[i, j - 1];
                }
            }
        }
        terrain = newTerrains;
    }

    private void MoveNegX()
    {
        if (terrain == null)
            return;
        Debug.Log("Move Neg x");
        GameObject[,] newTerrains = new GameObject[CHUNKS, CHUNKS];

        int n_zoffset = m_zOff;

        for (int i = 0; i < CHUNKS; i++)
        {
            terrain[2, i].transform.position = terrain[0, i].transform.position - new Vector3(width - 2, 0, 0);
            CalcNoise(terrain[2, i].GetComponent<Terrain>());
        }

        for (int i = 0; i < CHUNKS; i++)
        {
            for (int j = 0; j < CHUNKS; j++)
            {
                if (i == 0)
                    newTerrains[i, j] = terrain[2, j];
                else
                {
                    newTerrains[i, j] = terrain[i - 1, j];
                }
            }
        }
        terrain = newTerrains;



    }

    void MovePosX()
    {
        if (terrain == null)
            return;
        Debug.Log("Move Pos x");
        GameObject[,] newTerrains = new GameObject[CHUNKS, CHUNKS];
        int n_zoffset = m_zOff;
        m_zOff -= 3 * length;
        for (int i = 0; i < CHUNKS; i++)
        {
            terrain[0, i].transform.position = terrain[2, i].transform.position + new Vector3(width - 2, 0, 0);
            CalcNoise(terrain[0, i].GetComponent<Terrain>());
            m_zOff += length;
        }
        m_zOff = n_zoffset;

        for (int i = 0; i < CHUNKS; i++)
        {
            for (int j = 0; j < CHUNKS; j++)
            {
                if (i == 2)
                    newTerrains[i, j] = terrain[0, j];
                else
                {
                    newTerrains[i, j] = terrain[i + 1, j];
                }
            }
        }
        terrain = newTerrains;
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
                tData[col * CHUNKS + row].SetDetailResolution(ALPHA_TILE_SIZE, 16);
                tData[col * CHUNKS + row].baseMapResolution = width - 1 + 1;
                tData[col * CHUNKS + row].SetHeights(0, 0, heightMap[col * CHUNKS + row]);
                tData[col * CHUNKS + row].size = new Vector3(width - 1, height, length - 1);
                tData[col * CHUNKS + row].splatPrototypes = test;
                tData[col * CHUNKS + row].detailPrototypes = setDetails(tData[col * CHUNKS + row]);
                tData[col * CHUNKS + row].SetDetailLayer(0, 0, 0, getDetailLayers(tData[col * CHUNKS + row]));

                TreePrototype[] _treeprotos = new TreePrototype[_treeInstance.Length];
                for (int i = 0; i < _treeInstance.Length; i++)
                {
                    _treeprotos[i] = new TreePrototype();
                    _treeprotos[i].prefab = _treeInstance[i];
                    _treeprotos[i].bendFactor = 1f;
                    _treeprotos[i].prefab.renderer.material.color = Color.white;
                }

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
                terrain[row, col].GetComponent<Terrain>().Flush();
                terrain[row, col].GetComponent<Terrain>().terrainData.RefreshPrototypes();
            }
        }
    }
    int terrno = 0;
    public int noOfTreesPerTerrain = 0;
    #endregion

    #region AddDetail Prototypes
    public GameObject StonePrototype = null;
    DetailPrototype[] setDetails(TerrainData td)
    {
        DetailPrototype[] dProtos = new DetailPrototype[detailObjects.Length + 1];
        for (int i = 0; i < detailObjects.Length; i++)
        {
            dProtos[i] = new DetailPrototype();
            dProtos[i].prototypeTexture = detailObjects[i];
            dProtos[i].healthyColor = Color.white;
            dProtos[i].dryColor = Color.black;
            dProtos[i].noiseSpread = 0f;
            dProtos[i].maxHeight = 1;
            dProtos[i].maxWidth = 1;
            dProtos[i].renderMode = DetailRenderMode.Grass;
            dProtos[i].usePrototypeMesh = false;
        }
        dProtos[detailObjects.Length] = new DetailPrototype();
        dProtos[detailObjects.Length].prototype = StonePrototype;
        dProtos[detailObjects.Length].healthyColor = Color.white;
        dProtos[detailObjects.Length].dryColor = Color.black;
        dProtos[detailObjects.Length].noiseSpread = 0f;
        dProtos[detailObjects.Length].maxHeight = 1;
        dProtos[detailObjects.Length].maxWidth = 1;
        dProtos[detailObjects.Length].renderMode = DetailRenderMode.VertexLit;
        dProtos[detailObjects.Length].usePrototypeMesh = true;
        return dProtos;
    }

    int[,] getDetailLayers(TerrainData td)
    {

        int[,] details = new int[width, length];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < length; j++)
            {
                var ht = td.GetHeight(i, j);
                if (ht > height * 0.3 && ht < height * 0.335)
                {
                    var t = 1;
                    details[i, j] = t;
                }
                else
                {
                    if (ht < height * 0.3)
                    details[i, j] = 2;
                    Debug.Log("ROCK");
                }

                //if (i == width || j == length)
                    //details[i, j] = 1;
            }
        }

        return details;
    }
    #endregion

    #region ADDING TREES
    public GameObject water;
    void GenerateTrees()
    {
        int treesAdd = 0;
        foreach (var terr in terrain)
        {
            var t = terr.GetComponent<Terrain>();
          //  Debug.Log("Terrain Name: " + terr.name + "Added: " + treesAdd);
            treesAdd = 0;
            var totalTrees = noOfTreesPerTerrain;
            totalTrees += rand.Next(0, 30);
            for (int i = 0; i < totalTrees; i++)
            {
                Vector2 r = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble());
                var treeHt = (float)SimplexNoise.noise(r.x * rand.Next(0, width), r.y * rand.Next(0, length));
                treeHt = treeHt < 0 ? treeHt * -1 : treeHt;
                {
                    TreeInstance ti = new TreeInstance();
                    ti.prototypeIndex = rand.Next(0, _treeInstance.Length);
                    ti.position = new Vector3(r.x, 0, r.y);
                    ti.lightmapColor = Color.white;
                    ti.color = Color.white;
                    ti.heightScale = 2.5f;
                    ti.widthScale = 2.5f;
                    terr.GetComponent<Terrain>().castShadows = true;
                    terr.GetComponent<Terrain>().AddTreeInstance(ti);
                    treesAdd++;
                }
            }
        }
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
        globalDetailStructure = new int[width, height];
        // since we want the noise to be consistent based on the indices
        // of the map, we scale and offset them
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //Debug.Log(i);
                //Debug.Log(j);
                var w = height2d((positionX + i) * scale + this.m_xOff, (positionZ + j) * scale + this.m_zOff, perlinOctaves, 2.0f, gain) + zOffset;
                if (regen)
                    pos[i, j] = w;
                else
                    pos[i, j] += w;
                globalDetailStructure[i,j] = getDetailAtPoint(pos[i, j], 0);
            }
        }


        pos = normalizePerlin(ref pos, new Vector2(width, height));

        return pos;
    }

    int[,] globalDetailStructure;
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
        for (i = 0; i < voronoiCells; i += inc)
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
        for (My = 0; My < Ty; My += inc)
        {
            for (Mx = 0; Mx < Tx; Mx += inc)
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
        var heightMap = t.terrainData.GetHeights(0, 0, (int)width, (int)length);
        heightMap = k_perlin(heightMap, width, length, perlinGain, 0, m_xOff, m_zOff, true);
        NewTextures(t, heightMap, width, height);
        t.terrainData.RefreshPrototypes();
        t.terrainData.SetHeights(0, 0, heightMap);
        t.terrainData.SetDetailLayer(0, 0, 0, globalDetailStructure);
    }

    int getDetailAtPoint(float ht, int detail)
    {
        if (ht > 1)
            ht = ht / height;
        if (ht > height * 0.05 && ht < height * 0.135)
        {
            if (rand.Next(0, 10) < 2)
            {
                var t = 0;
                detail = t;
            }
        }
        else
        {
            if (ht < height * 0.3)
                if (rand.Next(0, 10) < 7)
                {
                    detail = 1;
                }
        }
        return detail;
    }

    private void NewTextures(Terrain t, float[,] heightMap, int width, int length)
    {
        for (int i = 0; i < width - 1; i++)
        {
            for (int j = 0; j < length - 1; j++)
            {
                #region RE - TEXTURING
                var CurrentHeight = heightMap[i, j];
                float[, ,] singlePoint = new float[1, 1, 7];
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

    #region STITCHING

    void stichAll(ref Terrain main, Terrain left, Terrain top, Terrain right, Terrain bottom)
    {
        try
        {
            float equalizer = 0;
            if (left != null)
            {
                var m_left = getLeft(main);
                var l_right = getRight(left);
                float[,] bufferLeft = new float[64, 1];
                for (int i = 0; i < 64; i++)
                {
                    try
                    {
                        equalizer += m_left[i, 0];
                        equalizer += l_right[i, 0];
                        var t21 = Vector2.Lerp(new Vector2(m_left[i, 0], 0), new Vector2(l_right[i, 0], 0), 1);
                        equalizer /= 4;
                        bufferLeft[i, 0] = t21.x;
                    }
                    catch (Exception _e)
                    {
                        Debug.Log(_e.Message);
                    }
                }
                try
                {
                    main.terrainData.SetHeights(0, 0, bufferLeft);
                }
                catch (Exception _e)
                {
                    Debug.Log(_e.Message);
                }
            }

            if (top != null)
            {

                var m_top = getTop(main);
                var t_bot = getBottom(top);
                equalizer = 0;
                float[,] bufferTop = new float[1, 64];
                for (int i = 0; i < 64; i++)
                {
                    try
                    {
                        equalizer += m_top[0, i];
                        equalizer += t_bot[0, i];
                        var t21 = Vector2.Lerp(new Vector2(m_top[0, i], 0), new Vector2(t_bot[0, i], 0), 1);
                        equalizer /=4;
                        bufferTop[0, i] = t21.x;
                    }
                    catch (Exception _e)
                    {
                        Debug.Log(_e.Message);
                    }
                } try
                {
                    main.terrainData.SetHeights(0, 64 - 1, bufferTop);
                }
                catch (Exception _e)
                {
                    Debug.Log(_e.Message);
                }
            }


            if (right != null)
            {
                var m_right = getRight(main);
                var r_left = getLeft(right);
                float[,] bufferRight = new float[64, 1];
                equalizer = 0;
                for (int i = 0; i < length; i++)
                {
                    try
                    {
                        equalizer += m_right[i, 0];
                        equalizer += r_left[i, 0];
                        equalizer /= 4;
                        var t21 = Vector2.Lerp(new Vector2(m_right[i,0 ], 0), new Vector2(r_left[i, 0], 0), 1);
                        
                        bufferRight[i, 0] = t21.x;
                    }
                    catch (Exception _e)
                    {
                        Debug.Log(_e.Message);
                    }
                }
                try
                {
                    main.terrainData.SetHeights(64 - 1, 0, bufferRight);
                }
                catch (Exception _e)
                {
                    Debug.Log(_e.Message);
                }
            }

            if (bottom != null)
            {
                var m_bot = getBottom(main);
                var b_top = getTop(bottom);
                equalizer = 0;
                float[,] bufferBottom = new float[1, 64];

                for (int i = 0; i < 64; i++)
                {
                    try
                    {
                        equalizer += m_bot[0, i];
                        equalizer += b_top[0, i];
                        equalizer /= 4;
                        var t21 = Vector2.Lerp(new Vector2(m_bot[0, i], 0), new Vector2(b_top[0, i], 0), 1);
                 
                        bufferBottom[0, i] = t21.x;
                    }
                    catch (Exception _e)
                    {
                        Debug.Log(_e.Message);
                    }
                }
                try
                {
                    main.terrainData.SetHeights(64, 1, bufferBottom);
                }
                catch (Exception _e)
                {
                    Debug.Log(_e.Message);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    private float[,] getLeft(Terrain t)
    {
        return t.terrainData.GetHeights(0, 0, 1, length);
    }

    float[,] getTop(Terrain t)
    {
        return t.terrainData.GetHeights(0, length - 1, width, 1);
    }

    float[,] getRight(Terrain t)
    {
        return t.terrainData.GetHeights(width - 1, 0, 1, length);
    }

    float[,] getBottom(Terrain t)
    {
        return t.terrainData.GetHeights(0, 0, width, 1);
    }

    private void loadBuffers()
    {

    }
    #endregion
}


#region SIMPLEX NOISE
// copied and modified from http://webstaff.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf

public class SimplexNoise
{  // Simplex noise in 2D, 3D and 4D
    private static int[][] grad3 = new int[][] {
                                 new int[] {1,1,0}, new int[] {-1,1,0}, new int[] {1,-1,0}, new int[] {-1,-1,0},
                                 new int[] {1,0,1}, new int[] {-1,0,1}, new int[] {1,0,-1}, new int[] {-1,0,-1},
                                 new int[] {0,1,1}, new int[] {0,-1,1}, new int[] {0,1,-1}, new int[] {0,-1,-1}};
    private static int[][] grad4 = new int[][] {
                   new int[] {0,1,1,1},  new int[] {0,1,1,-1},  new int[] {0,1,-1,1},  new int[] {0,1,-1,-1},
                   new int[] {0,-1,1,1}, new int[] {0,-1,1,-1}, new int[] {0,-1,-1,1}, new int[] {0,-1,-1,-1},
                   new int[] {1,0,1,1},  new int[] {1,0,1,-1},  new int[] {1,0,-1,1},  new int[] {1,0,-1,-1},
                   new int[] {-1,0,1,1}, new int[] {-1,0,1,-1}, new int[] {-1,0,-1,1}, new int[] {-1,0,-1,-1},
                   new int[] {1,1,0,1},  new int[] {1,1,0,-1},  new int[] {1,-1,0,1},  new int[] {1,-1,0,-1},
                   new int[] {-1,1,0,1}, new int[] {-1,1,0,-1}, new int[] {-1,-1,0,1}, new int[] {-1,-1,0,-1},
                   new int[] {1,1,1,0},  new int[] {1,1,-1,0},  new int[] {1,-1,1,0},  new int[] {1,-1,-1,0},
                   new int[] {-1,1,1,0}, new int[] {-1,1,-1,0}, new int[] {-1,-1,1,0}, new int[] {-1,-1,-1,0}};
    private static int[] p = {151,160,137,91,90,15,
  131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
  190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
  88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
  77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
  102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
  135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
  5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
  223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
  129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
  251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
  49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
  138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180};
    // To remove the need for index wrapping, double the permutation table length
    private static int[] perm = new int[512];
    static SimplexNoise() { for (int i = 0; i < 512; i++) perm[i] = p[i & 255]; } // moved to constructor
    // A lookup table to traverse the simplex around a given point in 4D.
    // Details can be found where this table is used, in the 4D noise method.
    private static int[][] simplex = new int[][] {
    new int[] {0,1,2,3}, new int[] {0,1,3,2}, new int[] {0,0,0,0}, new int[] {0,2,3,1}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {1,2,3,0},
    new int[] {0,2,1,3}, new int[] {0,0,0,0}, new int[] {0,3,1,2}, new int[] {0,3,2,1}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {1,3,2,0},
    new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0},
    new int[] {1,2,0,3}, new int[] {0,0,0,0}, new int[] {1,3,0,2}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {2,3,0,1}, new int[] {2,3,1,0},
    new int[] {1,0,2,3}, new int[] {1,0,3,2}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {2,0,3,1}, new int[] {0,0,0,0}, new int[] {2,1,3,0},
    new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0},
    new int[] {2,0,1,3}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {3,0,1,2}, new int[] {3,0,2,1}, new int[] {0,0,0,0}, new int[] {3,1,2,0},
    new int[] {2,1,0,3}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {3,1,0,2}, new int[] {0,0,0,0}, new int[] {3,2,0,1}, new int[] {3,2,1,0}};
    // This method is a *lot* faster than using (int)Mathf.floor(x)
    private static int fastfloor(double x)
    {
        return x > 0 ? (int)x : (int)x - 1;
    }
    private static double dot(int[] g, double x, double y)
    {
        return g[0] * x + g[1] * y;
    }
    private static double dot(int[] g, double x, double y, double z)
    {
        return g[0] * x + g[1] * y + g[2] * z;
    }
    private static double dot(int[] g, double x, double y, double z, double w)
    {
        return g[0] * x + g[1] * y + g[2] * z + g[3] * w;
    }  // 2D simplex noise
    public static double noise(double xin, double yin)
    {
        double n0, n1, n2; // Noise contributions from the three corners
        // Skew the input space to determine which simplex cell we're in
        double F2 = 0.5 * (Mathf.Sqrt(3.0f) - 1.0);
        double s = (xin + yin) * F2; // Hairy factor for 2D
        int i = fastfloor(xin + s);
        int j = fastfloor(yin + s);
        double G2 = (3.0 - Mathf.Sqrt(3.0f)) / 6.0;
        double t = (i + j) * G2;
        double X0 = i - t; // Unskew the cell origin back to (x,y) space
        double Y0 = j - t;
        double x0 = xin - X0; // The x,y distances from the cell origin
        double y0 = yin - Y0;
        // For the 2D case, the simplex shape is an equilateral triangle.
        // Determine which simplex we are in.
        int i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
        if (x0 > y0) { i1 = 1; j1 = 0; } // lower triangle, XY order: (0,0)->(1,0)->(1,1)
        else { i1 = 0; j1 = 1; }      // upper triangle, YX order: (0,0)->(0,1)->(1,1)
        // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
        // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
        // c = (3-Sqrt(3))/6
        double x1 = x0 - i1 + G2; // Offsets for middle corner in (x,y) unskewed coords
        double y1 = y0 - j1 + G2;
        double x2 = x0 - 1.0 + 2.0 * G2; // Offsets for last corner in (x,y) unskewed coords
        double y2 = y0 - 1.0 + 2.0 * G2;
        // Work out the hashed gradient indices of the three simplex corners
        int ii = i & 255;
        int jj = j & 255;
        int gi0 = perm[ii + perm[jj]] % 12;
        int gi1 = perm[ii + i1 + perm[jj + j1]] % 12;
        int gi2 = perm[ii + 1 + perm[jj + 1]] % 12;
        // Calculate the contribution from the three corners
        double t0 = 0.5 - x0 * x0 - y0 * y0;
        if (t0 < 0) n0 = 0.0;
        else
        {
            t0 *= t0;
            n0 = t0 * t0 * dot(grad3[gi0], x0, y0);  // (x,y) of grad3 used for 2D gradient
        }
        double t1 = 0.5 - x1 * x1 - y1 * y1;
        if (t1 < 0) n1 = 0.0;
        else
        {
            t1 *= t1;
            n1 = t1 * t1 * dot(grad3[gi1], x1, y1);
        } double t2 = 0.5 - x2 * x2 - y2 * y2;
        if (t2 < 0) n2 = 0.0;
        else
        {
            t2 *= t2;
            n2 = t2 * t2 * dot(grad3[gi2], x2, y2);
        }
        // Add contributions from each corner to get the final noise value.
        // The result is scaled to return values in the interval [-1,1].
        return 70.0 * (n0 + n1 + n2);
    }
    // 3D simplex noise
    public static double noise(double xin, double yin, double zin)
    {
        double n0, n1, n2, n3; // Noise contributions from the four corners
        // Skew the input space to determine which simplex cell we're in
        double F3 = 1.0 / 3.0;
        double s = (xin + yin + zin) * F3; // Very nice and simple skew factor for 3D
        int i = fastfloor(xin + s);
        int j = fastfloor(yin + s);
        int k = fastfloor(zin + s);
        double G3 = 1.0 / 6.0; // Very nice and simple unskew factor, too
        double t = (i + j + k) * G3;
        double X0 = i - t; // Unskew the cell origin back to (x,y,z) space
        double Y0 = j - t;
        double Z0 = k - t;
        double x0 = xin - X0; // The x,y,z distances from the cell origin
        double y0 = yin - Y0;
        double z0 = zin - Z0;
        // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
        // Determine which simplex we are in.
        int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
        int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords
        if (x0 >= y0)
        {
            if (y0 >= z0)
            { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // X Y Z order
            else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; } // X Z Y order
            else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; } // Z X Y order
        }
        else
        { // x0<y0
            if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } // Z Y X order
            else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } // Y Z X order
            else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // Y X Z order
        }
        // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
        // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
        // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
        // c = 1/6.
        double x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
        double y1 = y0 - j1 + G3;
        double z1 = z0 - k1 + G3;
        double x2 = x0 - i2 + 2.0 * G3; // Offsets for third corner in (x,y,z) coords
        double y2 = y0 - j2 + 2.0 * G3;
        double z2 = z0 - k2 + 2.0 * G3;
        double x3 = x0 - 1.0 + 3.0 * G3; // Offsets for last corner in (x,y,z) coords
        double y3 = y0 - 1.0 + 3.0 * G3;
        double z3 = z0 - 1.0 + 3.0 * G3;
        // Work out the hashed gradient indices of the four simplex corners
        int ii = i & 255;
        int jj = j & 255;
        int kk = k & 255;
        int gi0 = perm[ii + perm[jj + perm[kk]]] % 12;
        int gi1 = perm[ii + i1 + perm[jj + j1 + perm[kk + k1]]] % 12;
        int gi2 = perm[ii + i2 + perm[jj + j2 + perm[kk + k2]]] % 12;
        int gi3 = perm[ii + 1 + perm[jj + 1 + perm[kk + 1]]] % 12;
        // Calculate the contribution from the four corners
        double t0 = 0.6 - x0 * x0 - y0 * y0 - z0 * z0;
        if (t0 < 0) n0 = 0.0;
        else
        {
            t0 *= t0;
            n0 = t0 * t0 * dot(grad3[gi0], x0, y0, z0);
        }
        double t1 = 0.6 - x1 * x1 - y1 * y1 - z1 * z1;
        if (t1 < 0) n1 = 0.0;
        else
        {
            t1 *= t1;
            n1 = t1 * t1 * dot(grad3[gi1], x1, y1, z1);
        }
        double t2 = 0.6 - x2 * x2 - y2 * y2 - z2 * z2;
        if (t2 < 0) n2 = 0.0;
        else
        {
            t2 *= t2;
            n2 = t2 * t2 * dot(grad3[gi2], x2, y2, z2);
        }
        double t3 = 0.6 - x3 * x3 - y3 * y3 - z3 * z3;
        if (t3 < 0) n3 = 0.0;
        else
        {
            t3 *= t3;
            n3 = t3 * t3 * dot(grad3[gi3], x3, y3, z3);
        }
        // Add contributions from each corner to get the final noise value.
        // The result is scaled to stay just inside [-1,1]
        return 32.0 * (n0 + n1 + n2 + n3);
    }  // 4D simplex noise
    double noise(double x, double y, double z, double w)
    {

        // The skewing and unskewing factors are hairy again for the 4D case
        double F4 = (Mathf.Sqrt(5.0f) - 1.0) / 4.0;
        double G4 = (5.0 - Mathf.Sqrt(5.0f)) / 20.0;
        double n0, n1, n2, n3, n4; // Noise contributions from the five corners
        // Skew the (x,y,z,w) space to determine which cell of 24 simplices we're in
        double s = (x + y + z + w) * F4; // Factor for 4D skewing
        int i = fastfloor(x + s);
        int j = fastfloor(y + s);
        int k = fastfloor(z + s);
        int l = fastfloor(w + s);
        double t = (i + j + k + l) * G4; // Factor for 4D unskewing
        double X0 = i - t; // Unskew the cell origin back to (x,y,z,w) space
        double Y0 = j - t;
        double Z0 = k - t;
        double W0 = l - t;
        double x0 = x - X0;  // The x,y,z,w distances from the cell origin
        double y0 = y - Y0;
        double z0 = z - Z0;
        double w0 = w - W0;
        // For the 4D case, the simplex is a 4D shape I won't even try to describe.
        // To find out which of the 24 possible simplices we're in, we need to
        // determine the magnitude ordering of x0, y0, z0 and w0.
        // The method below is a good way of finding the ordering of x,y,z,w and
        // then find the correct traversal order for the simplex we’re in.
        // First, six pair-wise comparisons are performed between each possible pair
        // of the four coordinates, and the results are used to add up binary bits
        // for an integer index.
        int c1 = (x0 > y0) ? 32 : 0;
        int c2 = (x0 > z0) ? 16 : 0;
        int c3 = (y0 > z0) ? 8 : 0;
        int c4 = (x0 > w0) ? 4 : 0;
        int c5 = (y0 > w0) ? 2 : 0;
        int c6 = (z0 > w0) ? 1 : 0;
        int c = c1 + c2 + c3 + c4 + c5 + c6;
        int i1, j1, k1, l1; // The integer offsets for the second simplex corner
        int i2, j2, k2, l2; // The integer offsets for the third simplex corner
        int i3, j3, k3, l3; // The integer offsets for the fourth simplex corner
        // simplex[c] is a 4-vector with the numbers 0, 1, 2 and 3 in some order.
        // Many values of c will never occur, since e.g. x>y>z>w makes x<z, y<w and x<w
        // impossible. Only the 24 indices which have non-zero entries make any sense.
        // We use a thresholding to set the coordinates in turn from the largest magnitude.
        // The number 3 in the "simplex" array is at the position of the largest coordinate.
        i1 = simplex[c][0] >= 3 ? 1 : 0;
        j1 = simplex[c][1] >= 3 ? 1 : 0;
        k1 = simplex[c][2] >= 3 ? 1 : 0;
        l1 = simplex[c][3] >= 3 ? 1 : 0;
        // The number 2 in the "simplex" array is at the second largest coordinate.
        i2 = simplex[c][0] >= 2 ? 1 : 0;
        j2 = simplex[c][1] >= 2 ? 1 : 0; k2 = simplex[c][2] >= 2 ? 1 : 0;
        l2 = simplex[c][3] >= 2 ? 1 : 0;
        // The number 1 in the "simplex" array is at the second smallest coordinate.
        i3 = simplex[c][0] >= 1 ? 1 : 0;
        j3 = simplex[c][1] >= 1 ? 1 : 0;
        k3 = simplex[c][2] >= 1 ? 1 : 0;
        l3 = simplex[c][3] >= 1 ? 1 : 0;
        // The fifth corner has all coordinate offsets = 1, so no need to look that up.
        double x1 = x0 - i1 + G4; // Offsets for second corner in (x,y,z,w) coords
        double y1 = y0 - j1 + G4;
        double z1 = z0 - k1 + G4;
        double w1 = w0 - l1 + G4;
        double x2 = x0 - i2 + 2.0 * G4; // Offsets for third corner in (x,y,z,w) coords
        double y2 = y0 - j2 + 2.0 * G4;
        double z2 = z0 - k2 + 2.0 * G4;
        double w2 = w0 - l2 + 2.0 * G4;
        double x3 = x0 - i3 + 3.0 * G4; // Offsets for fourth corner in (x,y,z,w) coords
        double y3 = y0 - j3 + 3.0 * G4;
        double z3 = z0 - k3 + 3.0 * G4;
        double w3 = w0 - l3 + 3.0 * G4;
        double x4 = x0 - 1.0 + 4.0 * G4; // Offsets for last corner in (x,y,z,w) coords
        double y4 = y0 - 1.0 + 4.0 * G4;
        double z4 = z0 - 1.0 + 4.0 * G4;
        double w4 = w0 - 1.0 + 4.0 * G4;
        // Work out the hashed gradient indices of the five simplex corners
        int ii = i & 255;
        int jj = j & 255;
        int kk = k & 255;
        int ll = l & 255;
        int gi0 = perm[ii + perm[jj + perm[kk + perm[ll]]]] % 32;
        int gi1 = perm[ii + i1 + perm[jj + j1 + perm[kk + k1 + perm[ll + l1]]]] % 32;
        int gi2 = perm[ii + i2 + perm[jj + j2 + perm[kk + k2 + perm[ll + l2]]]] % 32;
        int gi3 = perm[ii + i3 + perm[jj + j3 + perm[kk + k3 + perm[ll + l3]]]] % 32;
        int gi4 = perm[ii + 1 + perm[jj + 1 + perm[kk + 1 + perm[ll + 1]]]] % 32;
        // Calculate the contribution from the five corners
        double t0 = 0.6 - x0 * x0 - y0 * y0 - z0 * z0 - w0 * w0;
        if (t0 < 0) n0 = 0.0;
        else
        {
            t0 *= t0;
            n0 = t0 * t0 * dot(grad4[gi0], x0, y0, z0, w0);
        }
        double t1 = 0.6 - x1 * x1 - y1 * y1 - z1 * z1 - w1 * w1;
        if (t1 < 0) n1 = 0.0;
        else
        {
            t1 *= t1;
            n1 = t1 * t1 * dot(grad4[gi1], x1, y1, z1, w1);
        }
        double t2 = 0.6 - x2 * x2 - y2 * y2 - z2 * z2 - w2 * w2;
        if (t2 < 0) n2 = 0.0;
        else
        {
            t2 *= t2;
            n2 = t2 * t2 * dot(grad4[gi2], x2, y2, z2, w2);
        } double t3 = 0.6 - x3 * x3 - y3 * y3 - z3 * z3 - w3 * w3;
        if (t3 < 0) n3 = 0.0;
        else
        {
            t3 *= t3;
            n3 = t3 * t3 * dot(grad4[gi3], x3, y3, z3, w3);
        }
        double t4 = 0.6 - x4 * x4 - y4 * y4 - z4 * z4 - w4 * w4;
        if (t4 < 0) n4 = 0.0;
        else
        {
            t4 *= t4;
            n4 = t4 * t4 * dot(grad4[gi4], x4, y4, z4, w4);
        }
        // Sum up and scale the result to cover the range [-1,1]
        return 27.0 * (n0 + n1 + n2 + n3 + n4);
    }
}
#endregion
