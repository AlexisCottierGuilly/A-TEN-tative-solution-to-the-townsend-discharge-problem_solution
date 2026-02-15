using UnityEngine;

public class SwitchSceneButton : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        GameManager.instance.LoadScene(sceneName);
    }
}
