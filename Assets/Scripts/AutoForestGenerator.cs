using System;
using System.Diagnostics;
using UnityEngine;

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
    // CONTEXT MENU
    // =======================

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
    // INTERNAL
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

            float normX = x / terrainSize.x;
            float normZ = z / terrainSize.z;

            float height01 = data.GetInterpolatedHeight(normX, normZ) / terrainSize.y;
            if (height01 < minHeight || height01 > maxHeight) continue;

            float slope = data.GetSteepness(normX, normZ);
            if (slope > maxSlope) continue;

            Vector3 worldPos = new Vector3(
                terrainPos.x + x,
                terrainPos.y + data.GetInterpolatedHeight(normX, normZ),
                terrainPos.z + z
            );

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            obj.transform.Rotate(0f, Random.Range(0f, 360f), 0f);
        }
    }
}
