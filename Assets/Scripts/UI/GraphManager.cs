using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GraphManager : MonoBehaviour
{
    public TextMeshProUGUI graphTitle;
    public TextMeshProUGUI labelX;
    public TextMeshProUGUI labelY;

    [Space]
    public List<TextMeshProUGUI> graduationsX;
    public List<TextMeshProUGUI> graduationsY;

    [Space]
    public RectTransform axes;
    public Transform dotsParent;

    public void SetTitle(string title) { graphTitle.text = title; }
    public void SetLabelX(string label) { labelX.text = label; }
    public void SetLabelY(string label) { labelY.text = label; }

    public void SetGraduationsX(List<float> graduations, string format = "F2")
    {
        for (int i = 0; i < graduationsX.Count; i++)
        {
            if (i < graduations.Count)
                graduationsX[i].text = graduations[i].ToString(format);
            else
                graduationsX[i].text = "";
        }
    }

    public void SetGraduationsY(List<float> graduations, string format = "F2")
    {
        for (int i = 0; i < graduationsY.Count; i++)
        {
            if (i < graduations.Count)
                graduationsY[i].text = graduations[i].ToString(format);
            else
                graduationsY[i].text = "";
        }
    }
}
