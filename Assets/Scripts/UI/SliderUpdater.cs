using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderUpdater : MonoBehaviour
{
    public TextMeshProUGUI valueText;

    void Start()
    {
        UpdateValueText();
    }

    public void ValueChanged()
    {
        UpdateValueText();
    }

    void UpdateValueText()
    {
        valueText.text = GetComponent<Slider>().value.ToString("F0");
    }
}
