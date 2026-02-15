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
    public bool cumulative = false;
    [Space]
    public float timeInterval = 0.01f;
    public float timeFrameInterval = 2e-8f;
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
    private GameObject graph1;
    private GameObject graph2;
    
    public int numFrames = 100;
    float frameInterval = 0f;
    public int currentFrame = 0;
    float timeSinceLastFrame = 0f;
    public int numSlices = 10;
    private List<List<Electron>> splitCollisionData;
    private int currentFrameIndex = 0;

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

        splitCollisionData = new List<List<Electron>>();
        SplitCollisionData();
        Debug.Log($"Splits: {splitCollisionData.Count} frames with intervals of {frameInterval} seconds");
        currentFrameIndex = 0;
        timeSinceLastFrame = 0f;

        if (splitCollisionData.Count > 0)
        {
            ParticleMap map = CollisionsToMap(splitCollisionData[0]);
            RenderMap(map);
        }

        if (graph1 != null)
        {
            uiManager.RemoveFromElements(graph1);
            Destroy(graph1);
        }

        if (graph2 != null)
        {
            uiManager.RemoveFromElements(graph2);
            Destroy(graph2);
        }

        GraphData data1 = GetCollisionAndDistanceData();
        graph1 = GraphCollisionsAndDistance(data1);
        uiManager.AddToElements(graph1);

        GraphData linearizedData = GetLinearizedCollisionData(data1);
        graph2 = graphGenerator.GenerateGraph(linearizedData, graphParent.transform);
        uiManager.AddToElements(graph2);
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
            currentFrameIndex++;
            if (currentFrameIndex >= splitCollisionData.Count)
            {
                currentFrameIndex = 0;
                timeSinceLastFrame = -1f;  // Pause 1 sec
            }

            List<Electron> collisionsToRender = cumulative ? new List<Electron>() : splitCollisionData[currentFrameIndex];
            if (cumulative)
            {
                for (int i=0; i<=currentFrameIndex; i++)
                {
                    collisionsToRender.AddRange(splitCollisionData[i]);
                }
            }

            ParticleMap map = CollisionsToMap(collisionsToRender);
            RenderMap(map);
            timeSinceLastFrame = 0f;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            for (int i=0; i<Mathf.Min(100, simulator.collisionPoints.Count); i++)
            {
                Electron collision = simulator.collisionPoints[i];
                Debug.Log("Collision " + i + ": Position=" + collision.position.ToString("F6") + ", Time=" + collision.time + "s, Energy=" + collision.energy.ToString("F4") + "eV, Order=" + collision.order);
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

    public ParticleMap CollisionsToMap(List<Electron> collisions)
    {
        ParticleMap map = new ParticleMap();
        map.width = textureSize.x;
        map.height = textureSize.y;
        map.typeColors = new Dictionary<int, Color>()
        {
            { 0, Color.white },
            { 1, Color.red }
        };

        map.particles = new List<int>();
        map.particlePositions = new List<Vector2>();

        foreach (var collision in collisions)
        {
            Vector2 initialPos = new Vector2(collision.position.x, collision.position.z);
            //convert to between 0 and 1
            Vector2 pos = new Vector2(
                (initialPos.x) / initialDistance * textureSize.x,
                (initialPos.y + initialDiameter / 2f) / initialDiameter * textureSize.y
            );

            int type = 0;//TODO: collision.Item3 ? 1 : 0;
            map.particles.Add(type);
            map.particlePositions.Add(pos);
        }

        return map;
    }

    public void SplitCollisionData()
    {
        float maxTime = 0f;
        foreach (Electron electron in simulator.collisionPoints)
        {
            if (electron.time > maxTime)
            {
                maxTime = electron.time;
            }
        }
        frameInterval = maxTime / numFrames;
        for (int i=0; i<numFrames; i++)
        {
            float frameStart = i * frameInterval;
            float frameEnd = (i + 1) * frameInterval;
            List<Electron> frameCollisions = new List<Electron>();
            foreach (Electron electron in simulator.collisionPoints)
            {
                if (electron.time >= frameStart && electron.time < frameEnd)
                {
                    frameCollisions.Add(electron);
                }
            }
            splitCollisionData.Add(frameCollisions);
        }
    }

    public GraphData GetLinearizedCollisionData(GraphData originalData)
    {
        GraphData data = new GraphData();
        data.title = "Log of Collisions Per Distance";
        data.labelX = originalData.labelX;
        data.labelY = "ln(" + originalData.labelY + ")";

        List<float> dataX = new List<float>();
        List<float> dataY = new List<float>();

        for (int i = 0; i < originalData.dataX.Count; i++)
        {
            dataX.Add(originalData.dataX[i]);
            dataY.Add(originalData.dataY[i] > 0 ? Mathf.Log(originalData.dataY[i]) : 0f);
        }

        data.dataX = dataX;
        data.dataY = dataY;

        return data;
    }

    public GraphData GetCollisionAndDistanceData()
    {
        GraphData data = new GraphData();
        data.title = "Collision Per Distance";
        data.labelX = "Distance (mm)";
        data.labelY = "Collisions";

        List<float> dataX = new List<float>();
        List<float> dataY = new List<float>();

        for (int i = 0; i < numSlices; i++)
        {
            float sliceStart = i * simulator.distance / numSlices;
            float sliceEnd = (i + 1) * simulator.distance / numSlices;

            float averageX = 0f;
            int collisionsInSlice = 0;
            foreach (Electron collision in simulator.collisionPoints)
            {
                float x = collision.position.x;
                if (x >= sliceStart && x < sliceEnd)
                {   
                    averageX += x;
                    collisionsInSlice++;
                }
            }
            averageX /= collisionsInSlice > 0 ? collisionsInSlice : 1;
            dataX.Add(averageX * 1000f); // convert to mm
            dataY.Add(collisionsInSlice);
        }

        data.dataX = dataX;
        data.dataY = dataY;

        return data;
    }

    public GameObject GraphCollisionsAndDistance(GraphData data)
    {
        int totalCollisions = simulator.collisionPoints.Count;

        List<float> lnNumCollisions = new List<float>();
        foreach (float collisions in data.dataY)
        {
            lnNumCollisions.Add(collisions > 0 ? Mathf.Log(collisions) : 0f);
        }

        Tuple<float, float, float> fit = LinearRegression(data.dataX, lnNumCollisions);
        Debug.Log("Linear fit: ln(Collisions) = " + fit.Item1.ToString("F4") + " * Distance + " + fit.Item2.ToString("F4") + " with R^2 = " + fit.Item3.ToString("F4"));

        GameObject graph = graphGenerator.GenerateGraph(data, graphParent.transform);

        return graph;
    }

    public Tuple<float, float, float> LinearRegression(List<float> x, List<float> y)
    {
        int n = x.Count;
        float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;

        for (int i = 0; i < n; i++)
        {
            sumX += x[i];
            sumY += y[i];
            sumXY += x[i] * y[i];
            sumX2 += x[i] * x[i];
        }

        float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        float intercept = (sumY - slope * sumX) / n;

        // Calculate R-squared
        float ssTot = 0f, ssRes = 0f;
        for (int i = 0; i < n; i++)
        {
            float predictedY = slope * x[i] + intercept;
            ssTot += (y[i] - sumY / n) * (y[i] - sumY / n);
            ssRes += (y[i] - predictedY) * (y[i] - predictedY);
        }
        float rSquared = 1f - ssRes / ssTot;

        return new Tuple<float, float, float>(slope, intercept, rSquared);
    }

    public GameObject GraphEnergyAndTime()
    {
        GraphData data = new GraphData();
        data.title = "Energy vs Time";
        data.labelX = "Time (s)";
        data.labelY = "Energy (eV)";

        List<float> dataX = new List<float>();
        List<float> dataY = new List<float>();

        for (int i = 0; i < numFrames; i++)
        {
            float averageTime = 0f;
            float averageEnergy = 0f;
            for (int j = 0; j < splitCollisionData[i].Count; j++)
            {
                averageTime += splitCollisionData[i][j].time;
                averageEnergy += splitCollisionData[i][j].energy;
            }
            averageTime /= splitCollisionData[i].Count;
            averageEnergy /= splitCollisionData[i].Count;
            dataX.Add(averageTime);
            dataY.Add(averageEnergy);
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
