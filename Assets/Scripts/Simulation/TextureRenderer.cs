using UnityEngine;
using System.Collections.Generic;

public class TextureRenderer : MonoBehaviour
{
    public bool enableTest = false;
    public Vector2Int textureSize = new Vector2Int(100, 100);
    public Vector2Int gridSize = new Vector2Int(10, 10);
    public Vector2 offset = Vector2.zero;
    public Vector2 previewSize = new Vector2(10f, 10f);
    public int particleCount = 100;
    private ParticleMap testMap;
    private GameObject testPreview;

    void Start()
    {
        if (enableTest)
        {
            ReloadTest();
        }
    }

    void Update()
    {
        if (enableTest && Input.GetKeyDown(KeyCode.Space))
        {
            ReloadTest();
        }
    }

    void ReloadTest()
    {
        if (testPreview != null)
        {
            Destroy(testPreview);
        }
        
        ParticleMap map = new ParticleMap();
        map.width = textureSize.x;
        map.height = textureSize.y;
        map.typeColors = new Dictionary<int, Color>()
        {
            { 0, Color.red },
            { 1, Color.green },
            { 2, Color.blue }
        };

        map.particles = new List<int>();
        map.particlePositions = new List<Vector2>();
        for (int i = 0; i < particleCount; i++)
        {
            map.particles.Add(Random.Range(0, 3));
            float x = Random.Range(0, map.width);
            float y = Random.Range(0, map.height);
            map.particlePositions.Add(new Vector2(x, y));
        }
        
        //Texture2D texture = TextureFromParticleMap(map);
        List<List<float>> grid = GetGridFromParticleMap(map, gridSize);

        Texture2D texture = TextureFromGrid(grid, new Vector2(map.width, map.height));
        testPreview = GetTextureGO(texture, parent: this.transform, previewSize: previewSize, offset: offset);
    }

    public GameObject GetTextureGO(Texture2D texture, Transform parent = null, Vector2 previewSize = default(Vector2), Vector2 offset = default(Vector2))
    {
        GameObject textureGO = new GameObject("ParticleTexture");
        textureGO.transform.SetParent(parent, false);
        SpriteRenderer renderer = textureGO.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        textureGO.transform.localScale = new Vector2(previewSize.x / texture.width, previewSize.y / texture.height);
        textureGO.transform.localPosition = offset;
        return textureGO;
    }

    public Texture2D TextureFromParticleMap(ParticleMap map, bool pixelPerfect = true, bool distorsion = false)
    {
        Texture2D texture = new Texture2D(map.width, map.height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        float distorsionForce = map.width / 15f;

        // calculate the average distance-based color for each pixel
        for (int i = 0; i < map.width; i++)
        {
            for (int j = 0; j < map.height; j++)
            {
                List<Color> colors = new List<Color>();
                List<float> weights = new List<float>();
                for (int k=0; k<map.particles.Count; k++)
                {
                    Vector2 currentPosition = new Vector2(i, j);
                    if (distorsion)
                    {
                        float normalizedJ = j / (float)map.height;
                        float distort = 1f + 4f * Mathf.Pow(normalizedJ - 0.5f, 2f);
                        currentPosition = new Vector2(i + distort * distorsionForce, j);
                    }

                    Vector2 particlePos = map.particlePositions[k];
                    float distance = Vector2.Distance(currentPosition, particlePos);

                    if (distance < 25f)
                    {
                        int type = map.particles[k];
                        if (map.typeColors.ContainsKey(type))
                        {
                            colors.Add(map.typeColors[type]);
                            weights.Add(1f / Mathf.Pow(distance / 5f + 1f, 2f) - 1f / 36f); // weight inversely proportional to distance
                        }
                    }
                }
                Color finalColor = BlendColors(colors, weights);
                if (colors.Count == 0)
                {
                    finalColor = Color.clear;
                }
                texture.SetPixel(i, j, finalColor);
            }
        }
        
        texture.Apply();

        return texture;
    }

    public Color BlendColors(List<Color> colors, List<float> weights)
    {
        Color blendedColor = Color.black;
        float totalWeight = 0f;

        for (int i = 0; i < colors.Count; i++)
        {
            blendedColor += colors[i] * weights[i];
            totalWeight += weights[i];
        }

        if (totalWeight > 0f)
        {
            blendedColor /= totalWeight;
        }

        return blendedColor;
    }

    public Texture2D TextureFromGrid(List<List<float>> grid, Vector2 size)
    {
        int width = grid[0].Count;
        int height = grid.Count;
        Texture2D texture = new Texture2D((int)size.x, (int)size.y, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        float maxGridValue = 0f;
        for (int i=0; i<height; i++)
        {
            for (int j=0; j<width; j++)
            {
                if (grid[i][j] > maxGridValue)
                {
                    maxGridValue = grid[i][j];
                }
            }
        }

        Vector2 gridCellConversion = new Vector2(
            size.x / (width - 1f),
            size.y / (height - 1f)
        );

        float cellWidth = size.x / (width - 1f);
        float cellHeight = size.y / (height - 1f);
        float cellArea = cellWidth * cellHeight;

        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                float gridX = i / gridCellConversion.x;
                float gridY = j / gridCellConversion.y;

                //gridX += (1f + 4f * Mathf.Pow((gridY / (height - 1f)) - 0.5f, 2f)) * width / 35f;

                int intGridX = Mathf.Clamp(Mathf.FloorToInt(gridX), 0, width - 2);
                int intGridY = Mathf.FloorToInt(gridY);

                float corner1 = grid[intGridY][intGridX];
                float corner2 = grid[intGridY][intGridX + 1];
                float corner3 = grid[intGridY + 1][intGridX];
                float corner4 = grid[intGridY + 1][intGridX + 1];

                float dx = gridX - intGridX;
                float dy = gridY - intGridY;

                float value1 = (((1f - dx) * (1f - dy))) * corner1;
                float value2 = ((dx * (1f - dy))) * corner2;
                float value3 = ((1f - dx) * dy) * corner3;
                float value4 = (dx * dy) * corner4;

                float value = value1 + value2 + value3 + value4;
                value /= maxGridValue;

                Color color = new Color(value, value, value, 1f);
                texture.SetPixel(i, j, color);
            }
        }

        texture.Apply();
        return texture;
    }

    public List<List<float>> GetGridFromParticleMap(ParticleMap map, Vector2Int gridSize)
    {
        List<List<float>> grid = new List<List<float>>();

        for (int i = 0; i < gridSize.y; i++)
        {
            List<float> row = new List<float>();
            for (int j = 0; j < gridSize.x; j++)
            {
                row.Add(0f);
            }
            grid.Add(row);
        }

        float cellWidth = map.width / (float)gridSize.x;
        float cellHeight = map.height / (float)gridSize.y;
        float cellArea = cellWidth * cellHeight;

        foreach (Vector2 particlePos in map.particlePositions)
        {
            Vector2 particleGridPos = new Vector2(
                particlePos.x / map.width * (gridSize.x - 1),
                particlePos.y / map.height * (gridSize.y - 1)
            );
            
            int x = Mathf.FloorToInt(particleGridPos.x);
            int y = Mathf.FloorToInt(particleGridPos.y);

            float dx = particleGridPos.x - x;
            float dy = particleGridPos.y - y;

            // float area1Value = (cellWidth - dx) * (cellHeight - dy) / cellArea;
            // float area2Value = dx * (cellHeight - dy) / cellArea;
            // float area3Value = (cellWidth - dx) * dy / cellArea;
            // float area4Value = dx * dy / cellArea;

            float area1Value = 1f - (dx * dy) / cellArea;
            float area2Value = 1f - (dx * (cellHeight - dy)) / cellArea;
            float area3Value = 1f - ((cellWidth - dx) * dy) / cellArea;
            float area4Value = 1f - ((cellWidth - dx) * (cellHeight - dy)) / cellArea;

            grid[y][x] += area1Value;
            grid[y][x + 1] += area2Value;
            grid[y + 1][x] += area3Value;
            grid[y + 1][x + 1] += area4Value;
        }

        return grid;
    }
}


[System.Serializable]
public class ParticleMap
{
    public int width;
    public int height;

    public Dictionary<int, Color> typeColors;
    public List<int> particles;
    public List<Vector2> particlePositions;
}
