using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(Terrain))]
public class ProceduralIslandTextures : MonoBehaviour
{
    [Header("Niveau de l'eau")]
    [Tooltip("Hauteur mondiale Y de la surface de l'eau.")]
    public float waterLevel = 0f;

    [Header("Largeur des zones")]
    [Min(0f)]
    [Tooltip("Hauteur de la bande de sable mouillé au-dessus de l'eau.")]
    public float wetSandHeight = 1.5f;

    [Min(0f)]
    [Tooltip("Hauteur de la bande de sable sec.")]
    public float drySandHeight = 3f;

    [Min(0.01f)]
    [Tooltip("Largeur des transitions entre les matériaux.")]
    public float transitionWidth = 0.75f;

    [Header("Irrégularité des bordures")]
    public bool useNoise = true;

    [Min(0.001f)]
    public float noiseScale = 0.02f;

    [Min(0f)]
    [Tooltip("Décalage vertical maximal provoqué par le bruit.")]
    public float noiseStrength = 0.6f;

    public Vector2 noiseOffset;

    [Header("Gestion des pentes")]
    [Tooltip("Force progressivement la terre sur les pentes importantes.")]
    public bool useSlope = true;

    [Range(0f, 90f)]
    public float dirtSlopeStart = 30f;

    [Range(0f, 90f)]
    public float dirtSlopeFull = 55f;

    [Header("Indices des Terrain Layers")]
    public int wetSandLayer = 0;
    public int drySandLayer = 1;
    public int dirtLayer = 2;

    private Terrain terrain;
    private TerrainData terrainData;

    [ContextMenu("Générer les textures")]
    public void GenerateTextures()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;

        if (terrainData == null)
        {
            Debug.LogError("Aucun TerrainData trouvé.", this);
            return;
        }

        int layerCount = terrainData.alphamapLayers;

        if (layerCount < 3)
        {
            Debug.LogError(
                "Le Terrain doit posséder au moins trois Terrain Layers : " +
                "sable mouillé, sable sec et terre.",
                this
            );
            return;
        }

        if (!IsValidLayer(wetSandLayer, layerCount) ||
            !IsValidLayer(drySandLayer, layerCount) ||
            !IsValidLayer(dirtLayer, layerCount))
        {
            Debug.LogError("Un des indices de Terrain Layer est invalide.", this);
            return;
        }

#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(
            terrainData,
            "Génération procédurale des textures du terrain"
        );
#endif

        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;

        // Format : [coordonnée Y, coordonnée X, couche]
        float[,,] alphamaps = new float[height, width, layerCount];

        float terrainWorldY = terrain.transform.position.y;

        float wetSandEnd = waterLevel + wetSandHeight;
        float drySandEnd = wetSandEnd + drySandHeight;

        for (int y = 0; y < height; y++)
        {
            float normalizedY = y / (float)(height - 1);

            for (int x = 0; x < width; x++)
            {
                float normalizedX = x / (float)(width - 1);

                float localHeight = terrainData.GetInterpolatedHeight(
                    normalizedX,
                    normalizedY
                );

                float worldHeight = terrainWorldY + localHeight;

                float heightOffset = 0f;

                if (useNoise)
                {
                    float worldX =
                        terrain.transform.position.x +
                        normalizedX * terrainData.size.x;

                    float worldZ =
                        terrain.transform.position.z +
                        normalizedY * terrainData.size.z;

                    float noise = Mathf.PerlinNoise(
                        worldX * noiseScale + noiseOffset.x,
                        worldZ * noiseScale + noiseOffset.y
                    );

                    // Convertit le bruit de 0..1 vers -1..1.
                    heightOffset = (noise * 2f - 1f) * noiseStrength;
                }

                float modifiedHeight = worldHeight + heightOffset;

                float wetWeight =
                    1f - SmoothStep(
                        wetSandEnd - transitionWidth,
                        wetSandEnd + transitionWidth,
                        modifiedHeight
                    );

                float dryWeight =
                    SmoothStep(
                        wetSandEnd - transitionWidth,
                        wetSandEnd + transitionWidth,
                        modifiedHeight
                    )
                    *
                    (
                        1f - SmoothStep(
                            drySandEnd - transitionWidth,
                            drySandEnd + transitionWidth,
                            modifiedHeight
                        )
                    );

                float dirtWeight =
                    SmoothStep(
                        drySandEnd - transitionWidth,
                        drySandEnd + transitionWidth,
                        modifiedHeight
                    );

                if (useSlope)
                {
                    float slope = terrainData.GetSteepness(
                        normalizedX,
                        normalizedY
                    );

                    float slopeDirtWeight = Mathf.InverseLerp(
                        dirtSlopeStart,
                        dirtSlopeFull,
                        slope
                    );

                    // Réduit le sable sur les pentes fortes.
                    wetWeight *= 1f - slopeDirtWeight;
                    dryWeight *= 1f - slopeDirtWeight;

                    dirtWeight = Mathf.Max(dirtWeight, slopeDirtWeight);
                }

                float totalWeight = wetWeight + dryWeight + dirtWeight;

                if (totalWeight <= 0.0001f)
                {
                    dirtWeight = 1f;
                    totalWeight = 1f;
                }

                wetWeight /= totalWeight;
                dryWeight /= totalWeight;
                dirtWeight /= totalWeight;

                alphamaps[y, x, wetSandLayer] = wetWeight;
                alphamaps[y, x, drySandLayer] = dryWeight;
                alphamaps[y, x, dirtLayer] = dirtWeight;
            }
        }

        terrainData.SetAlphamaps(0, 0, alphamaps);

#if UNITY_EDITOR
        EditorUtility.SetDirty(terrainData);
#endif

        Debug.Log("Textures procédurales du terrain générées.", this);
    }

    private bool IsValidLayer(int layerIndex, int layerCount)
    {
        return layerIndex >= 0 && layerIndex < layerCount;
    }

    private float SmoothStep(float min, float max, float value)
    {
        if (Mathf.Approximately(min, max))
            return value >= max ? 1f : 0f;

        float t = Mathf.InverseLerp(min, max, value);
        return t * t * (3f - 2f * t);
    }
}

