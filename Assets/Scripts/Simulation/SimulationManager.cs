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
        List<Tuple<Vector3, float>> dataSample;
        int totalCollisions = simulator.collisionPoints.Count;

        Debug.Log(totalCollisions);
    }

    public void ReducedElectrificationChanged() { simulator.reducedEfield = reducedElectrificationSlider.value; }
    public void ElectrodeDistanceChanged() { simulator.distance = electrodeDistanceSlider.value; }
    public void PressureChanged() { simulator.pressure = pressureSlider.value; }
}
