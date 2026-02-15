using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class SimulationManager : MonoBehaviour
{
    public MonteCarlo simulator;
    public GraphGenerator graphGenerator;
    public GameObject graphParent;

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

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            GraphCollisionsAndDistance();
        }
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
