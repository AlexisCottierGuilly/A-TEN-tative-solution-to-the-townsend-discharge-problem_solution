using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public enum HeatMapType
{
    Zone,
    Point
}

public class SimulationManager : MonoBehaviour
{
    public MonteCarlo simulator;
    public TextureRenderer textureRenderer;
    public GraphGenerator graphGenerator;
    public SimulationUIManager uiManager;
    public GameObject graphParent;

    [Header("Simulation Render")]
    public HeatMapType heatMapType = HeatMapType.Zone;
    [Space]
    public float timeInterval = 0.5f;
    public float dataFrameInterval = 1e-8f;
    [Space]
    public Vector2Int textureSize = new Vector2Int(100, 100);
    public Vector2Int gridSize = new Vector2Int(10, 10);
    public Vector2 previewOffset = new Vector2(0f, 0f);
    public Vector2 previewSize = new Vector2(10f, 10f);
    [Space]
    public Transform previewParent;

    [Space]
    public Slider reducedElectrificationSlider;
    public Slider electrodeDistanceSlider;
    public Slider pressureSlider;

    private GameObject preview;
    private GameObject graph;
    private List<List<Tuple<Vector3, float, bool>>> splitCollisionData;
    private int currentFrameIndex = 0;
    private float timeSinceLastFrame = 0f;

    private float initialDistance;
    private float initialDiameter;

    void Start()
    {
        ReducedElectrificationChanged();
        ElectrodeDistanceChanged();
        PressureChanged();
    }

    public void SimulationDidFinish()
    {
        Debug.Log("SIMULATION FINISHED");
        initialDistance = simulator.distance;
        initialDiameter = simulator.diameter;

        splitCollisionData = SplitCollisionData(dataFrameInterval);
        currentFrameIndex = 0;

        if (splitCollisionData.Count > 0)
        {
            ParticleMap map = CollisionsToMap(splitCollisionData[0]);
            RenderMap(map);
        }

        if (graph != null)
        {
            uiManager.removeFromElements(graph);
            Destroy(graph);
        }

        graph = GraphCollisionsAndDistance();
        uiManager.AddToElements(graph);
    }

    public void SimulationDidStart()
    {
        Debug.Log("SIMULATION STARTED");
        if (preview != null)
        {
            Destroy(preview);
        }
    }

    void Update()
    {
        timeSinceLastFrame += Time.deltaTime;

        if (splitCollisionData != null && currentFrameIndex < splitCollisionData.Count && timeSinceLastFrame >= timeInterval)
        {
            List<Tuple<Vector3, float, bool>> collisions = new List<Tuple<Vector3, float, bool>>();

            for (int i=0; i<=currentFrameIndex; i++)
            {
                collisions.AddRange(splitCollisionData[i]);
            }

            ParticleMap map = CollisionsToMap(collisions);
            RenderMap(map);
            currentFrameIndex++;
            timeSinceLastFrame = 0f;

            if (currentFrameIndex >= splitCollisionData.Count)
            {
                currentFrameIndex = 0;
                timeSinceLastFrame = -1f;  // Pause 1 sec
            }
        }
    }

    public void RenderMap(ParticleMap map)
    {
        if (preview != null)
        {
            Destroy(preview);
        }

        Texture2D texture;
        if (heatMapType == HeatMapType.Zone)
        {
            List<List<float>> grid = textureRenderer.GetGridFromParticleMap(map, gridSize);
            texture = textureRenderer.TextureFromGrid(grid, new Vector2(map.width, map.height));
        }
        else
        {
            texture = textureRenderer.TextureFromParticles(map, blur: false);
        }

        preview = textureRenderer.GetTextureGO(texture, parent: previewParent, previewSize: previewSize, offset: previewOffset);
        preview.transform.position = new Vector3(preview.transform.position.x, preview.transform.position.y, 10f);
    }

    public ParticleMap CollisionsToMap(List<Tuple<Vector3, float, bool>> collisions)
    {
        ParticleMap map = new ParticleMap();
        map.width = gridSize.x;
        map.height = gridSize.y;
        map.typeColors = new Dictionary<int, Color>()
        {
            { 0, Color.white },
            { 1, Color.red }
        };

        map.particles = new List<int>();
        map.particlePositions = new List<Vector2>();

        foreach (var collision in collisions)
        {
            Vector2 pos = CollisionDataToPosition(collision);
            pos = new Vector2(
                pos.x / initialDistance * gridSize.x,
                pos.y / initialDiameter * gridSize.y
            );

            int type = collision.Item3 ? 1 : 0;
            map.particles.Add(type);
            map.particlePositions.Add(pos);
        }

        return map;
    }

    public List<List<Tuple<Vector3, float, bool>>> SplitCollisionData(float frameInterval)
    {
        List<List<Tuple<Vector3, float, bool>>> splitData = new List<List<Tuple<Vector3, float, bool>>>();
        List<Tuple<Vector3, float, bool>> currentFrame = new List<Tuple<Vector3, float, bool>>();
        float maxTimeStamp = frameInterval;
        float frameMaxTime = frameInterval;
        foreach (var collision in simulator.collisionPoints)
        {
            if (collision.Item2 > frameMaxTime)
            {
                splitData.Add(currentFrame);
                currentFrame = new List<Tuple<Vector3, float, bool>>();
                frameMaxTime += frameInterval;
            }
            currentFrame.Add(collision);
        }
        if (currentFrame.Count > 0)
        {
            splitData.Add(currentFrame);
        }

        Debug.Log($"{splitData.Count} frames from {simulator.collisionPoints.Count} collisions with frame interval {frameInterval}s");

        return splitData;
    }

    public Vector2 CollisionDataToPosition(Tuple<Vector3, float, bool> collision)
    {
        Vector3 pos = collision.Item1;
        return new Vector2(pos.z, pos.x);
    }

    public GameObject GraphCollisionsAndDistance()
    {
        int totalCollisions = simulator.collisionPoints.Count;

        GraphData data = new GraphData();
        data.title = "Collision Per Distance";
        data.labelX = "Distance (mm)";
        data.labelY = "Collisions";

        List<float> dataX = new List<float>();
        List<float> dataY = new List<float>();

        List<Tuple<Vector3, float, bool>> dataSample;
        dataSample = new();
        int sampleSize = Mathf.Min(1000, totalCollisions);
        for (int i = 0; i < sampleSize; i++)
        {
            int index = (int)(i * (float)totalCollisions / sampleSize);
            dataX.Add(simulator.collisionPoints[index].Item1.z * 1000f); // convert to mm
            dataY.Add(UnityEngine.Random.Range(0f, 1f)); // each sample point represents one collision
        }

        data.dataX = dataX;
        data.dataY = dataY;

        GameObject graph = graphGenerator.GenerateGraph(data, graphParent.transform);
        return graph;
    }

    public void ReducedElectrificationChanged() { simulator.reducedEfield = reducedElectrificationSlider.value; }
    public void ElectrodeDistanceChanged() { simulator.distance = electrodeDistanceSlider.value / 1000f; }
    public void PressureChanged() { simulator.pressure = pressureSlider.value; }
}
