using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class SimulationManager : MonoBehaviour
{
    public MonteCarlo simulator;
    public GraphGenerator graphGenerator;

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

        List<Tuple<Vector3, float>> dataSample;
        //for (int i=0; i<Mathf.Min(100, ))

        Debug.Log(totalCollisions);
    }

    public void ReducedElectrificationChanged() { simulator.reducedEfield = reducedElectrificationSlider.value; }
    public void ElectrodeDistanceChanged() { simulator.distance = electrodeDistanceSlider.value / 1000f; }
    public void PressureChanged() { simulator.pressure = pressureSlider.value; }
}
