using UnityEngine;
using System.Collections;

public class perlinBG : MonoBehaviour {
    private Terrain t;
    public GameObject terrain;
    public TerrainData td;
    int x, y;
    public float perlinFrequency;
    public float perlinAmplitude;
    public float scale;
    public float xOffset;
    public float yOffset;
    public int perlinOctaves;
	public float height;
	public float sysFrequency;
	// Use this for initialization
	void Start () {
        t = terrain.GetComponent<Terrain>();
        td = t.terrainData;
        float[,] heights = new float[(int)td.size.x, (int)td.size.z];
		System.Random rand = new System.Random();
        for(int i = 0 ; i < td.size.x; i++)
        {
            for(int j = 0 ; j < td.size.z; j++)
            {
               // heights[i, j] += ((float) SimplexNoise.noise(i, j))>0?(float) SimplexNoise.noise(i, j):0;
            }
        }
        normalizePerlin(ref heights, new Vector2(td.size.x - 1, td.size.z - 1));
        td.SetHeights(0, 0, heights);
		td.RefreshPrototypes();
		t.Flush();
	}

	void Update () {
	
	}

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
                heightMap[Mx, My] *= height;
                averageHeight += (double)heightMap[Mx, My] == double.NaN ? 0 : (decimal)heightMap[Mx, My];
            }
        }

        // return the height map
        return heightMap;
    }

  
    #endregion

}
