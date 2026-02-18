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

    [Header("Counts")]
    public int treeCount = 2000;
    public int bushCount = 3000;

    // =======================
    // SPAWN DENSITY BOOST
    // =======================
    [Header("Spawn Density Boost")]
    [Range(1f, 20f)] public float treeSpawnMultiplier = 6f;
    [Range(1f, 20f)] public float bushSpawnMultiplier = 12f;

    // =======================
    // MINIMUM CLUSTER SPAWN
    // =======================
    [Header("Minimum Cluster Spawn")]
    [Range(0f, 1f)] public float minTreeClusterWeight = 0.25f;
    [Range(0f, 1f)] public float minBushClusterWeight = 0.45f;

    [Header("Placement Rules")]
    [Range(0f, 1f)] public float minHeight = 0.2f;
    [Range(0f, 1f)] public float maxHeight = 0.8f;
    [Range(0f, 60f)] public float maxSlope = 35f;

    [Header("Cluster Settings")]
    public int clusterCount = 12;
    public float clusterRadius = 40f;
    [Range(0f, 1f)] public float clusterEdgeFalloff = 1f;

    [Header("Gameplay Safe Zones")]
    public Vector3[] safeZoneCenters;
    public float safeZoneRadius = 15f;

    [Header("Path / Road Safe Zones")]
    public Vector3[] pathPoints;
    public float pathRadius = 10f;

    [Header("Performance")]
    public Transform viewer;
    public float maxSpawnDistance = 300f;

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

        SpawnObjects(treePrefabs,
            Mathf.RoundToInt(treeCount * treeSpawnMultiplier),
            size, pos, data, false);

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

        while (clusterCenters.Count < clusterCount && safety++ < clusterCount * 20)
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

    void SpawnObjects(
        GameObject[] prefabs,
        int count,
        Vector3 size,
        Vector3 pos,
        TerrainData data,
        bool isBush)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(0f, size.x);
            float z = Random.Range(0f, size.z);

            float nx = x / size.x;
            float nz = z / size.z;

            if (data.GetSteepness(nx, nz) > maxSlope) continue;

            Vector3 worldPos = new(
                pos.x + x,
                pos.y + data.GetInterpolatedHeight(nx, nz),
                pos.z + z
            );

            if (viewer && Vector3.Distance(viewer.position, worldPos) > maxSpawnDistance) continue;
            if (IsInsideSafeZone(worldPos) || IsInsidePath(worldPos)) continue;

            float weight = GetClusterWeight(worldPos);
            float finalWeight = Mathf.Max(
                weight,
                isBush ? minBushClusterWeight : minTreeClusterWeight
            );

            if (Random.value > finalWeight) continue;

            Instantiate(
                prefabs[Random.Range(0, prefabs.Length)],
                worldPos,
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
