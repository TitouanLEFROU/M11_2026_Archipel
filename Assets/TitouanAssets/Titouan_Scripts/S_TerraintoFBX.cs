using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;

public class TerrainToFBX : MonoBehaviour
{
    [Header("Terrain à exporter")]
    public Terrain terrain;

    [Header("Résolution du mesh")]
    [Tooltip("Nombre de vertices par côté. 513 = bon compromis.")]
    public int resolution = 513;

    [Header("Export")]
    public string fileName = "Terrain_Export";

    [ContextMenu("Export Terrain to FBX")]
    public void ExportTerrain()
    {
        if (terrain == null)
        {
            Debug.LogError("Aucun Terrain assigné.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;

        int heightmapResolution = terrainData.heightmapResolution;

        // On limite la résolution à celle réellement disponible.
        resolution = Mathf.Clamp(resolution, 2, heightmapResolution);

        Debug.Log(
            $"Export du Terrain : {terrainData.size.x} x {terrainData.size.z} " +
            $"avec une résolution de {resolution}x{resolution}"
        );

        GameObject meshObject = CreateTerrainMesh(
            terrain,
            resolution
        );

        if (meshObject == null)
        {
            Debug.LogError("Impossible de créer le mesh.");
            return;
        }

        // Demande à Unity où sauvegarder le FBX
        string path = EditorUtility.SaveFilePanel(
            "Exporter le Terrain en FBX",
            "Assets",
            fileName,
            "fbx"
        );

        if (string.IsNullOrEmpty(path))
        {
            DestroyImmediate(meshObject);
            return;
        }

        // Convertit le chemin absolu en chemin Unity relatif
        string projectPath = Application.dataPath;

        if (path.StartsWith(projectPath))
        {
            path = "Assets" + path.Substring(projectPath.Length);
        }
        else
        {
            Debug.LogError(
                "Le fichier doit être sauvegardé dans le dossier Assets du projet Unity."
            );

            DestroyImmediate(meshObject);
            return;
        }

        // Export FBX
        ModelExporter.ExportObject(path, meshObject);

        Debug.Log("Terrain exporté avec succès : " + path);

        // Supprime l'objet temporaire
        DestroyImmediate(meshObject);

        AssetDatabase.Refresh();
    }

    private GameObject CreateTerrainMesh(
        Terrain terrain,
        int resolution
    )
    {
        TerrainData terrainData = terrain.terrainData;

        Vector3 terrainSize = terrainData.size;

        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector2[] uvs = new Vector2[resolution * resolution];

        int[] triangles =
            new int[(resolution - 1) * (resolution - 1) * 6];

        int vertexIndex = 0;

        // --------------------------------------------------
        // VERTICES
        // --------------------------------------------------

        for (int z = 0; z < resolution; z++)
        {
            float normalizedZ =
                (float)z / (resolution - 1);

            for (int x = 0; x < resolution; x++)
            {
                float normalizedX =
                    (float)x / (resolution - 1);

                // Récupération de la hauteur interpolée
                float height = terrainData.GetInterpolatedHeight(
                    normalizedX,
                    normalizedZ
                );

                float posX =
                    normalizedX * terrainSize.x;

                float posZ =
                    normalizedZ * terrainSize.z;

                vertices[vertexIndex] =
                    new Vector3(
                        posX,
                        height,
                        posZ
                    );

                uvs[vertexIndex] =
                    new Vector2(
                        normalizedX,
                        normalizedZ
                    );

                vertexIndex++;
            }
        }

        // --------------------------------------------------
        // TRIANGLES
        // --------------------------------------------------

        int triangleIndex = 0;

        for (int z = 0; z < resolution - 1; z++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int i =
                    z * resolution + x;

                // Premier triangle
                triangles[triangleIndex++] = i;
                triangles[triangleIndex++] = i + resolution;
                triangles[triangleIndex++] = i + 1;

                // Deuxième triangle
                triangles[triangleIndex++] = i + 1;
                triangles[triangleIndex++] = i + resolution;
                triangles[triangleIndex++] = i + resolution + 1;
            }
        }

        // --------------------------------------------------
        // CREATION DU MESH
        // --------------------------------------------------

        Mesh mesh = new Mesh();

        mesh.name = terrain.name + "_Mesh";

        // Permet de dépasser 65k vertices
        mesh.indexFormat =
            UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // --------------------------------------------------
        // GAMEOBJECT TEMPORAIRE
        // --------------------------------------------------

        GameObject meshObject =
            new GameObject(terrain.name + "_Export");

        MeshFilter meshFilter =
            meshObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            meshObject.AddComponent<MeshRenderer>();

        meshFilter.sharedMesh = mesh;

        // Matériau neutre temporaire
        meshRenderer.sharedMaterial =
            new Material(Shader.Find("Standard"));

        // Position du mesh = position du Terrain
        meshObject.transform.position =
            terrain.transform.position;

        meshObject.transform.rotation =
            terrain.transform.rotation;

        meshObject.transform.localScale =
            terrain.transform.localScale;

        return meshObject;
    }
}