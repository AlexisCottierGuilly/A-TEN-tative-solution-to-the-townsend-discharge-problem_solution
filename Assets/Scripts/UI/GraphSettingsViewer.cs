using UnityEngine;
using UnityEngine.UI;

public class GraphSettingsViewer : MonoBehaviour
{
    public Canvas canvas;
    public GraphGenerator graphGenerator;
    public GameObject graphParent;
    public GraphData testData;

    [Space]
    public Vector2 offset = new Vector2(0f, 0f);

    void Awake()
    {
        GameObject graph = graphGenerator.GenerateGraph(testData, graphParent.transform);
        Vector2 modifiedOffset = new Vector2(offset.x * canvas.scaleFactor, offset.y * canvas.scaleFactor);
        graph.transform.position += new Vector3(modifiedOffset.x, modifiedOffset.y, 0f);
    }
}
