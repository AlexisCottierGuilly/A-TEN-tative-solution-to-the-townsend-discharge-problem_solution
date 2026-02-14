using UnityEngine;
using System.Collections.Generic;

public class GraphGenerator : MonoBehaviour
{
    public GameObject graphTemplate;
    public GameObject dotTemplate;
    public Transform graphParent;

    [Header("Testing")]
    public bool enableTest = false;
    public GraphData testData;

    void Start()
    {
        if (enableTest)
        {
            GenerateGraph(testData);
        }
    }

    public GameObject GenerateGraph(GraphData data)
    {
        GameObject newGraph = Instantiate(graphTemplate, graphParent);
        newGraph.SetActive(true);

        GraphManager manager = newGraph.GetComponent<GraphManager>();

        manager.SetTitle(data.title);
        manager.SetLabelX(data.labelX);
        manager.SetLabelY(data.labelY);

        Vector2 limitsX = GetDataLimits(data.dataX);
        Vector2 limitsY = GetDataLimits(data.dataY);
        manager.SetGraduationsX(GetGraduationsFromData(data.dataX, 13, limitsX), format: "F1");
        manager.SetGraduationsY(GetGraduationsFromData(data.dataY, 5, limitsY), format: "F1");

        AddPointsToGraph(newGraph, data.dataX, data.dataY, limitsX, limitsY);
        
        return newGraph;
    }

    public void AddPointsToGraph(GameObject graph, List<float> dataX, List<float> dataY, Vector2 limitsX, Vector2 limitsY)
    {
        GraphManager manager = graph.GetComponent<GraphManager>();
        for (int i = 0; i < Mathf.Min(dataX.Count, dataY.Count); i++)
        {
            GameObject dot = Instantiate(dotTemplate, manager.dotsParent);
            dot.SetActive(true);

            float normalizedX = (dataX[i] - limitsX.x) / (limitsX.y - limitsX.x);
            float normalizedY = (dataY[i] - limitsY.x) / (limitsY.y - limitsY.x);

            Vector2 graphRelativeZero = manager.axes.localPosition - new Vector3(manager.axes.rect.width * 0.435f, manager.axes.rect.height * 0.3675f);
            Vector2 dotPosition = new Vector2(normalizedX * manager.axes.rect.width * 0.808f, normalizedY * manager.axes.rect.height * 0.6125f) + graphRelativeZero;
            RectTransform rt = dot.GetComponent<RectTransform>();
            rt.anchoredPosition = dotPosition;
        }
    }

    public Vector2 GetDataLimits(List<float> data)
    {
        if (data.Count == 0) return Vector2.zero;

        float min = Mathf.Min(data.ToArray());
        float max = Mathf.Max(data.ToArray());

        return new Vector2(min, max);
    }

    public List<float> GetGraduationsFromData(List<float> data, int count, Vector2? limits = null)
    {
        List<float> graduations = new List<float>();
        if (data.Count == 0) return graduations;

        float min = limits.HasValue ? limits.Value.x : Mathf.Min(data.ToArray());
        float max = limits.HasValue ? limits.Value.y : Mathf.Max(data.ToArray());
        float step = (max - min) / (count - 1);

        for (int i = 0; i < count; i++)
        {
            graduations.Add(min + i * step);
        }

        return graduations;
    }
}


[System.Serializable]
public class GraphData
{
    public List<float> dataX;
    public List<float> dataY;

    [Space]
    public string title;
    public string labelX;
    public string labelY;
}
