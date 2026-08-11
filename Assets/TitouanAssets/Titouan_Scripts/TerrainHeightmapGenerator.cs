using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TerrainHeightmapGenerator : MonoBehaviour
{
    [Header("Terrain")]
    public Terrain targetTerrain;

    [Header("Output")]
    public string fileName = "IslandHeightmap.png";

    [ContextMenu("Generate Heightmap")]
    public void GenerateHeightmap()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("No Terrain assigned.");
            return;
        }

        TerrainData terrainData = targetTerrain.terrainData;

        int resolution = terrainData.heightmapResolution;

        float[,] heights = terrainData.GetHeights(
            0,
            0,
            resolution,
            resolution
        );

        Texture2D texture = new Texture2D(
            resolution,
            resolution,
            TextureFormat.R16,
            false,
            true
        );

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float height = heights[z, x];

                texture.SetPixel(
                    x,
                    z,
                    new Color(height, height, height, 1f)
                );
            }
        }

        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();

        string folder =
            Application.dataPath +
            "/TitouanAssets/TerrainData";

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string path =
            folder +
            "/" +
            fileName;

        File.WriteAllBytes(path, bytes);

        Debug.Log(
            "Terrain heightmap generated at: " +
            path
        );

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        DestroyImmediate(texture);
    }
}