using UnityEngine;
using System.Collections;
using System;

public class VoroniBG : MonoBehaviour {
    TerrainData td = null;
    float mHeight = 0f;
	// Use this for initialization
	void Start () {
        if (td == null)
            td = this.GetComponent<Terrain>().terrainData;
        runOnce();
	}

    void runOnce()
    {
        mHeight = td.size.y;
        //var current = this.td.GetHeights(0, 0, (int)this.td.size.x, (int)this.td.size.z);
        var current = new float[(int)this.td.size.x, (int)this.td.size.z];
        current = generateVoronoi(ref current, new Vector2(this.td.size.x - 1, this.td.size.z - 1), false);
        current = generateVoronoi(ref current, new Vector2(this.td.size.x - 1, this.td.size.z - 1), false); 
        this.td.SetHeights(0, 0, current);
        this.td.RefreshPrototypes();
        Debug.Log("SUCCESS");
    }
	// Update is called once per frame
	void Update () {
		//runOnce();
	}

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
    public int voronoiCells;
    public float voronoiScale;
    public float voronoiFeatures;
    //		voronoiPresets.Add(new voronoiPresetData("Scattered Peaks", VoronoiType.Linear, 16, 8, 0.5f, 1.0f));
    //	voronoiPresets.Add(new voronoiPresetData("Rolling Hills", VoronoiType.Sine, 8, 8, 0.0f, 1.0f));
    //	voronoiPresets.Add(new voronoiPresetData("Jagged Mountains", VoronoiType.Linear, 32, 32, 0.5f, 1.0f));
    private float[,] generateVoronoi(ref float[,] heightMap, Vector2 arraySize, bool regenFlag)
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
                heightMap[Mx, My] += hScore;
                if (hScore > highestScore)
                {
                    highestScore = hScore;
                }
                heightMap[Mx, My] = hScore + heightMap[Mx, My] < 1? heightMap[Mx, My] : 1;
                heightMap[Mx, My] *= mHeight;
            }

        }

        // Normalise...
        //for (My = 0; My < Ty; My++)
        //{
        //    for (Mx = 0; Mx < Tx; Mx++)
        //    {
        //        float normalisedHeight = heightMap[Mx, My] * (1.0f / highestScore);
        //        heightMap[Mx, My] = normalisedHeight;
        //    }
        //}
        return heightMap;
    }

    #endregion
}
