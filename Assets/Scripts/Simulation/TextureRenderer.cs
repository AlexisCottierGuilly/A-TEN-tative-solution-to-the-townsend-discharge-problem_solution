using UnityEngine;
using System.Collections.Generic;

public class TextureRenderer : MonoBehaviour
{
    public bool enableTest = false;
    public float previewScale = 1f;
    public int particleCount = 100;
    private ConcentrationMap testMap;

    void Start()
    {
        if (enableTest)
        {
            ConcentrationMap map = new ConcentrationMap();
            map.width = 100;
            map.height = 100;
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
                map.particlePositions.Add(new Vector2(Random.Range(0, map.width), Random.Range(0, map.height)));
            }

            GetTextureGO(map, parent: this.transform);
        }
    }

    public void GetTextureGO(ConcentrationMap map, Transform parent = null)
    {
        Texture2D texture = TextureFromConcentrationMap(map);
        GameObject textureGO = new GameObject("ConcentrationTexture");
        textureGO.transform.SetParent(parent, false);
        SpriteRenderer renderer = textureGO.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        textureGO.transform.localScale = Vector3.one * previewScale;
    }

    public Texture2D TextureFromConcentrationMap(ConcentrationMap map, bool pixelPerfect = true)
    {
        Texture2D texture = new Texture2D(map.width, map.height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        // calculate the average distance-based color for each pixel
        for (int i = 0; i < map.width; i++)
        {
            for (int j = 0; j < map.height; j++)
            {
                List<Color> colors = new List<Color>();
                List<float> weights = new List<float>();
                for (int k=0; k<map.particles.Count; k++)
                {
                    Vector2 particlePos = map.particlePositions[k];
                    float distance = Vector2.Distance(new Vector2(i, j), particlePos);

                    int type = map.particles[k];
                    if (map.typeColors.ContainsKey(type))
                    {
                        colors.Add(map.typeColors[type]);
                        weights.Add(1f / (distance / 5f + 1f)); // weight inversely proportional to distance
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
}


[System.Serializable]
public class ConcentrationMap
{
    public int width;
    public int height;

    public Dictionary<int, Color> typeColors;
    public List<int> particles;
    public List<Vector2> particlePositions;
}
