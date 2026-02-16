using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Autoplay : MonoBehaviour
{
    public bool enabled = true;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        if (enabled)
            button.onClick.Invoke();
    }
}
