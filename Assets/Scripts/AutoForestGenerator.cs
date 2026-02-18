using System;
using UnityEngine;

using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class AutoForestGenerator : MonoBehaviour
{
    [Header("Terrain")]
    public Terrain terrain;

    [Header("Prefabs")]
    public GameObject[] treePrefabs;
    public GameObject[] bushPrefabs;

    [Header("Base Counts")]
    public int treeCount = 3000;   // 🔥 ARTIRILDI
    public int bushCount = 5000;   // 🔥 ARTIRILDI

    // =======================
    // SPAWN MULTIPLIERS
    // =======================
    [Header("Spawn Multipliers")]
    [Range(1f, 30f)] public float treeSpawnMultiplier = 15f;   // 🔥 ÇOK YOĞUN
    [Range(1f, 30f)] public float bushSpawnMultiplier = 20f;   // 🔥 ÇOK YOĞUN

    // =======================
    // EXTRA PASSES
    // =======================
    [Header("Extra Passes")]
    [Range(1, 10)] public int treeSpawnPasses = 5;   // 🔥 AĞAÇ DOLDURUR
    [Range(1, 10)] public int bushSpawnPasses = 3;   // 🔥 ÇALI DOLDURUR

    // =======================
    // MINIMUM SPAWN (TABAN)
    // =======================
    [Header("Minimum Spawn Chance")]
    [Range(0f, 1f)] public float minTreeClusterWeight = 0.9f;  // 🔥 NEREDEYSE GARANTİ
    [Range(0f, 1f)] public float minBushClusterWeight = 0.85f;

    // =======================
    // HEIGHT DISTRIBUTION
    // =======================
    [Header("Height Distribution")]
    [Range(0f, 1f)] public float preferredHeightCenter = 0.55f;
    [Range(0f, 0.6f)] public float preferredHeightRange = 0.45f; // 🔥 ÇOK GENİŞ

    [Header("Placement Rules")]
    [Range(0f, 1f)] public float minHeight = 0.15f;
    [Range(0f, 1f)] public float maxHeight = 0.9f;
    [Range(0f, 60f)] public float maxSlope = 40f;

    [Header("Cluster Settings")]
    public int clusterCount = 14;
    public float clusterRadius = 55f;
    public float clusterEdgeFalloff = 0.8f;

    [Header("Safe Zones")]
    public Vector3[] safeZoneCenters;
    public float safeZoneRadius = 15f;

    [Header("Paths")]
    public Vector3[] pathPoints;
    public float pathRadius = 10f;

    private readonly System.Collections.Generic.List<Vector3> clusterCenters = new();

    [ContextMenu("Generate Forest")]
    public void GenerateForest()
    {
        if (terrain == null) return;
        ClearForest();

        var data = terrain.terrainData;
        var size = data.size;
        var pos = terrain.transform.position;

        GenerateClusterCenters(data, size, pos);

        // 🌲 AĞAÇLAR
        for (int p = 0; p < treeSpawnPasses; p++)
            SpawnObjects(treePrefabs,
                Mathf.RoundToInt(treeCount * treeSpawnMultiplier),
                size, pos, data, false);

        // 🌿 ÇALILAR
        for (int p = 0; p < bushSpawnPasses; p++)
            SpawnObjects(bushPrefabs,
                Mathf.RoundToInt(bushCount * bushSpawnMultiplier),
                size, pos, data, true);
    }

    [ContextMenu("Clear Forest")]
    public void ClearForest()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    void GenerateClusterCenters(TerrainData data, Vector3 size, Vector3 pos)
    {
        clusterCenters.Clear();
        int safety = 0;

        while (clusterCenters.Count < clusterCount && safety++ < clusterCount * 30)
        {
            float x = Random.Range(0f, size.x);
            float z = Random.Range(0f, size.z);

            float nx = x / size.x;
            float nz = z / size.z;

            float h01 = data.GetInterpolatedHeight(nx, nz) / size.y;
            if (h01 < minHeight || h01 > maxHeight) continue;
            if (data.GetSteepness(nx, nz) > maxSlope) continue;

            clusterCenters.Add(new Vector3(
                pos.x + x,
                pos.y + data.GetInterpolatedHeight(nx, nz),
                pos.z + z
            ));
        }
    }

    float GetClusterWeight(Vector3 p)
    {
        float best = 0f;
        foreach (var c in clusterCenters)
        {
            float d = Vector3.Distance(p, c);
            if (d > clusterRadius) continue;

            float w = 1f - Mathf.Clamp01(d / clusterRadius);
            w = Mathf.Pow(w, clusterEdgeFalloff);
            if (w > best) best = w;
        }
        return best;
    }

    float GetHeightWeight(float h01)
    {
        float dist = Mathf.Abs(h01 - preferredHeightCenter);
        float t = Mathf.Clamp01(dist / preferredHeightRange);
        return 1f - t;
    }

    void SpawnObjects(
        GameObject[] prefabs,
        int count,
        Vector3 size,
        Vector3 pos,
        TerrainData data,
        bool isBush)
    {
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(0f, size.x);
            float z = Random.Range(0f, size.z);

            float nx = x / size.x;
            float nz = z / size.z;

            float h01 = data.GetInterpolatedHeight(nx, nz) / size.y;
            if (h01 < minHeight || h01 > maxHeight) continue;
            if (data.GetSteepness(nx, nz) > maxSlope) continue;

            Vector3 p = new(
                pos.x + x,
                pos.y + data.GetInterpolatedHeight(nx, nz),
                pos.z + z
            );

            if (IsInsideSafeZone(p) || IsInsidePath(p)) continue;

            float weight =
                (GetClusterWeight(p) * 0.7f) +
                (GetHeightWeight(h01) * 0.7f);

            float finalWeight = Mathf.Clamp01(
                Mathf.Max(weight, isBush ? minBushClusterWeight : minTreeClusterWeight)
            );

            if (Random.value > finalWeight) continue;

            Instantiate(
                prefabs[Random.Range(0, prefabs.Length)],
                p,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                transform
            );
        }
    }

    bool IsInsideSafeZone(Vector3 p)
    {
        if (safeZoneCenters == null) return false;
        foreach (var c in safeZoneCenters)
            if (Vector3.Distance(p, c) <= safeZoneRadius) return true;
        return false;
    }

    bool IsInsidePath(Vector3 p)
    {
        if (pathPoints == null) return false;
        foreach (var c in pathPoints)
            if (Vector3.Distance(p, c) <= pathRadius) return true;
        return false;
    }
}
