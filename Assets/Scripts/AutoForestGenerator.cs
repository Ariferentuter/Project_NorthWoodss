using System;
using UnityEngine;

// Unity namespace’lerini sabitle
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

    [Header("Placement Rules")]
    [Range(0f, 1f)] public float minHeight = 0.2f;
    [Range(0f, 1f)] public float maxHeight = 0.8f;
    [Range(0f, 60f)] public float maxSlope = 35f;

    // =======================
    // CLUSTER SETTINGS
    // =======================
    [Header("Cluster Settings")]
    public int clusterCount = 12;

    [Header("Cluster Radius")]
    public float clusterRadius = 40f;

    [Header("Cluster Density")]
    [Range(0f, 1f)]
    public float clusterEdgeFalloff = 1f;

    // =======================
    // GAMEPLAY SAFE ZONES
    // =======================
    [Header("Gameplay Safe Zones")]
    public Vector3[] safeZoneCenters;
    public float safeZoneRadius = 15f;

    // =======================
    // PATH / ROAD SAFE ZONES
    // =======================
    [Header("Path / Road Safe Zones")]
    public Vector3[] pathPoints;
    public float pathRadius = 10f;

    // =======================
    // PERFORMANCE
    // =======================
    [Header("Performance")]
    public Transform viewer;              // Camera / Player
    public float maxSpawnDistance = 300f;

    private readonly System.Collections.Generic.List<Vector3> clusterCenters
        = new System.Collections.Generic.List<Vector3>();

    [ContextMenu("Generate Forest")]
    public void GenerateForest()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain atanmamış!");
            return;
        }

        ClearForest();

        TerrainData data = terrain.terrainData;
        Vector3 terrainSize = data.size;
        Vector3 terrainPos = terrain.transform.position;

        GenerateClusterCenters(data, terrainSize, terrainPos);

        SpawnObjects(treePrefabs, treeCount, terrainSize, terrainPos, data);
        SpawnObjects(bushPrefabs, bushCount, terrainSize, terrainPos, data);

        Debug.Log("Forest generated");
    }

    [ContextMenu("Clear Forest")]
    public void ClearForest()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        Debug.Log("Forest cleared");
    }

    [ContextMenu("Rebuild Forest")]
    public void RebuildForest()
    {
        ClearForest();
        GenerateForest();
    }

    // =======================
    // CLUSTER CENTER LOGIC
    // =======================
    void GenerateClusterCenters(TerrainData data, Vector3 terrainSize, Vector3 terrainPos)
    {
        clusterCenters.Clear();

        int safety = 0;
        int maxAttempts = clusterCount * 10;

        while (clusterCenters.Count < clusterCount && safety < maxAttempts)
        {
            safety++;

            float x = Random.Range(0f, terrainSize.x);
            float z = Random.Range(0f, terrainSize.z);

            float nx = x / terrainSize.x;
            float nz = z / terrainSize.z;

            float h01 = data.GetInterpolatedHeight(nx, nz) / terrainSize.y;
            if (h01 < minHeight || h01 > maxHeight) continue;

            float slope = data.GetSteepness(nx, nz);
            if (slope > maxSlope) continue;

            Vector3 worldPos = new Vector3(
                terrainPos.x + x,
                terrainPos.y + data.GetInterpolatedHeight(nx, nz),
                terrainPos.z + z
            );

            clusterCenters.Add(worldPos);
        }

        Debug.Log($"Cluster centers generated: {clusterCenters.Count}");
    }

    // =======================
    // CLUSTER DENSITY
    // =======================
    float GetClusterWeight(Vector3 worldPos)
    {
        float best = 0f;

        for (int i = 0; i < clusterCenters.Count; i++)
        {
            float d = Vector3.Distance(worldPos, clusterCenters[i]);
            if (d > clusterRadius) continue;

            float t = d / clusterRadius;
            float w = 1f - Mathf.Clamp01(t);
            w = Mathf.Pow(w, clusterEdgeFalloff);

            if (w > best) best = w;
        }
        return best;
    }

    // =======================
    // SAFE ZONE
    // =======================
    bool IsInsideSafeZone(Vector3 worldPos)
    {
        if (safeZoneCenters == null || safeZoneCenters.Length == 0)
            return false;

        for (int i = 0; i < safeZoneCenters.Length; i++)
        {
            if (Vector3.Distance(worldPos, safeZoneCenters[i]) <= safeZoneRadius)
                return true;
        }
        return false;
    }

    // =======================
    // PATH
    // =======================
    bool IsInsidePath(Vector3 worldPos)
    {
        if (pathPoints == null || pathPoints.Length == 0)
            return false;

        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (Vector3.Distance(worldPos, pathPoints[i]) <= pathRadius)
                return true;
        }
        return false;
    }

    // =======================
    // DISTANCE CULLING
    // =======================
    bool IsWithinSpawnDistance(Vector3 worldPos)
    {
        if (viewer == null)
            return true; // geri uyumluluk

        return Vector3.Distance(viewer.position, worldPos) <= maxSpawnDistance;
    }

    // =======================
    // SPAWN SYSTEM (OPTIMIZED)
    // =======================
    void SpawnObjects(
        GameObject[] prefabs,
        int count,
        Vector3 terrainSize,
        Vector3 terrainPos,
        TerrainData data)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(0f, terrainSize.x);
            float z = Random.Range(0f, terrainSize.z);

            float nx = x / terrainSize.x;
            float nz = z / terrainSize.z;

            float h01 = data.GetInterpolatedHeight(nx, nz) / terrainSize.y;
            if (h01 < minHeight || h01 > maxHeight) continue;

            float slope = data.GetSteepness(nx, nz);
            if (slope > maxSlope) continue;

            Vector3 worldPos = new Vector3(
                terrainPos.x + x,
                terrainPos.y + data.GetInterpolatedHeight(nx, nz),
                terrainPos.z + z
            );

            // 🚀 DISTANCE CULLING
            if (!IsWithinSpawnDistance(worldPos)) continue;

            // 🚫 SAFE ZONE
            if (IsInsideSafeZone(worldPos)) continue;

            // 🛣️ PATH
            if (IsInsidePath(worldPos)) continue;

            // 🌲 CLUSTER GATE
            float weight = GetClusterWeight(worldPos);
            if (Random.value > weight) continue;

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            obj.transform.Rotate(0f, Random.Range(0f, 360f), 0f);
        }
    }
}
