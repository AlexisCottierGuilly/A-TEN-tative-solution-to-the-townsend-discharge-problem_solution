using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class SimulationManager : MonoBehaviour
{
    public MonteCarlo simulator;
    public TextureRenderer textureRenderer;
    public GraphGenerator graphGenerator;
    public GameObject graphParent;

    [Header("Simulation Render")]
    public float timeInterval = 0.5f;
    public float dataFrameInterval = 0.1f;
    [Space]
    public Vector2Int textureSize = new Vector2Int(100, 100);
    public Vector2Int gridSize = new Vector2Int(10, 10);
    public Vector2 previewOffset = new Vector2(0f, 0f);
    public Vector2 previewSize = new Vector2(10f, 10f);

    [Space]
    public Slider reducedElectrificationSlider;
    public Slider electrodeDistanceSlider;
    public Slider pressureSlider;

    void Start()
    {
        ReducedElectrificationChanged();
        ElectrodeDistanceChanged();
        PressureChanged();
    }

    public void SimulationDidFinish()
    {
        Debug.Log("SIMULATION FINISHED");
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            GraphCollisionsAndDistance();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            
        }
    }

    public List<List<Tuple<Vector3, float, bool>>> SplitCollisionData(float frameInterval)
    {
        List<List<Tuple<Vector3, float, bool>>> splitData = new List<List<Tuple<Vector3, float, bool>>>();
        List<Tuple<Vector3, float, bool>> currentFrame = new List<Tuple<Vector3, float, bool>>();
        float lastTimestamp = 0f;
        foreach (var collision in simulator.collisionPoints)
        {
            if (collision.Item2 - lastTimestamp > frameInterval)
            {
                splitData.Add(currentFrame);
                currentFrame = new List<Tuple<Vector3, float, bool>>();
                lastTimestamp = collision.Item2;
            }
            currentFrame.Add(collision);
        }
        if (currentFrame.Count > 0)
        {
            splitData.Add(currentFrame);
        }
        return splitData;
    }

    public void GraphCollisionsAndDistance()
    {
        int totalCollisions = simulator.collisionPoints.Count;

        GraphData data = new GraphData();
        data.title = "Collision Per Distance";
        data.labelX = "Distance (mm)";
        data.labelY = "Collisions";

        List<float> dataX = new List<float>();
        List<float> dataY = new List<float>();

        List<Tuple<Vector3, float>> dataSample;
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
    }

    public void ReducedElectrificationChanged() { simulator.reducedEfield = reducedElectrificationSlider.value; }
    public void ElectrodeDistanceChanged() { simulator.distance = electrodeDistanceSlider.value / 1000f; }
    public void PressureChanged() { simulator.pressure = pressureSlider.value; }
}
