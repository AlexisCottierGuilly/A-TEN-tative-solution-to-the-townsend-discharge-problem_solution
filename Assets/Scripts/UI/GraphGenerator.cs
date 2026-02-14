using UnityEngine;
using System.Collections.Generic;

public class GraphGenerator : MonoBehaviour
{
    public GameObject graphTemplate;
    public GameObject dotTemplate;
    public GameObject lineTemplate;
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
        data.SortData();

        GameObject newGraph = Instantiate(graphTemplate, graphParent);
        newGraph.SetActive(true);

        GraphManager manager = newGraph.GetComponent<GraphManager>();

        manager.SetTitle(data.title);
        manager.SetLabelX(data.labelX);
        manager.SetLabelY(data.labelY);

        Vector2 limitsX = GetSmartLimits(data.dataX, 13);
        Vector2 limitsY = GetSmartLimits(data.dataY, 5);
        manager.SetGraduationsX(GetGraduationsFromData(data.dataX, 13, limitsX), format: "F1");
        manager.SetGraduationsY(GetGraduationsFromData(data.dataY, 5, limitsY), format: "F1");

        AddPointsToGraph(newGraph, data.dataX, data.dataY, limitsX, limitsY, lines: true);
        
        return newGraph;
    }

    public void AddPointsToGraph(GameObject graph, List<float> dataX, List<float> dataY, Vector2 limitsX, Vector2 limitsY, bool lines=false)
    {
        GraphManager manager = graph.GetComponent<GraphManager>();

        Vector2 previousPosition = Vector2.zero;
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

            // Add line to previous point
            if (lines && i > 0)
            {
                GameObject line = Instantiate(lineTemplate, manager.linesParent);
                line.SetActive(true);

                Vector2 direction = (dotPosition - previousPosition).normalized;
                float distance = Vector2.Distance(dotPosition, previousPosition);

                RectTransform lineRT = line.GetComponent<RectTransform>();
                lineRT.sizeDelta = new Vector2(distance, lineRT.rect.height); // 3f is the thickness of the line
                lineRT.anchoredPosition = previousPosition + direction * distance * 0.5f;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                lineRT.rotation = Quaternion.Euler(0, 0, angle);
            }

            previousPosition = dotPosition;
        }
    }

    public Vector2 GetDataLimits(List<float> data)
    {
        if (data.Count == 0) return Vector2.zero;

        float min = Mathf.Min(data.ToArray());
        float max = Mathf.Max(data.ToArray());

        return new Vector2(min, max);
    }

    public Vector2 GetSmartLimits(List<float> data, int graduationCount)
    {
        if (data.Count == 0) return Vector2.zero;

        float min = Mathf.Min(data.ToArray());
        float max = Mathf.Max(data.ToArray());

        float range = max - min;
        float optimalStep = range / (graduationCount - 1);

        float step;
        if (1f < optimalStep && optimalStep <= 10f)
        {
            float diff = optimalStep - Mathf.Floor(optimalStep);
            if (diff < 0.5f)
                step = Mathf.Floor(optimalStep) + 0.5f;
            else
                step = Mathf.Ceil(optimalStep);
        }
        else if (optimalStep > 10f)
        {
            float diff = optimalStep - Mathf.Floor(optimalStep / 10f) * 10f;
            step = Mathf.Floor(optimalStep) + (diff < 5f ? 5f : 10f);
        }
        else
        {
            step = optimalStep;
        }

        Debug.Log($"Range: {range}, Step: {step}");

        return new Vector2(min, min + step * (graduationCount - 1));
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
    public string title;
    public string labelX;
    public string labelY;

    [Space]
    public List<float> dataX;
    public List<float> dataY;

    public void SortData()
    {
        List<(float, float)> pairs = new List<(float, float)>();
        for (int i = 0; i < Mathf.Min(dataX.Count, dataY.Count); i++)
        {
            pairs.Add((dataX[i], dataY[i]));
        }

        pairs.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        for (int i = 0; i < pairs.Count; i++)
        {
            dataX[i] = pairs[i].Item1;
            dataY[i] = pairs[i].Item2;
        }
    }
}
