using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

public class SimulationUIManager : MonoBehaviour
{
    public Canvas canvas;
    [Space]
    public List<GameObject> elements = new List<GameObject>();
    public float spacing = 2.5f;
    public float scaleFactor = 1f;

    public Vector2 center = new Vector2(0f, 0f);

    private int scrollState = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ScrollUp();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ScrollDown();
        }
    }

    public void AddToElements(GameObject element)
    {
        float lastY = 0f;
        if (elements.Count > 0)
        {
            GameObject lastElement = elements[elements.Count - 1];
            if (IsGraphElement(lastElement))
            {
                lastY = GetUIElementBottomY(lastElement.GetComponent<GraphManager>().axes.gameObject) + WorldToUIScale(spacing);
            }
            else if (IsWorldElement(lastElement))
            {
                lastY = GetWorldElementBottomY(lastElement) + spacing;
            }
        }

        elements.Add(element);

        if (IsGraphElement(element))
        {
            Transform transform = element.GetComponent<Transform>();
            lastY -= UIToWorldScale(element.GetComponent<GraphManager>().axes.rect.height) * 5f;
            transform.position = new Vector3(WorldToUIScale(center.x) * 4.5f, WorldToUIScale(lastY), transform.position.z);
        }
        else if (IsWorldElement(element))
        {
            Transform transform = element.GetComponent<Transform>();
            lastY -= transform.localScale.y / 2f;
            transform.position = new Vector3(center.x, lastY, transform.position.z);
        }
    }

    public void removeFromElements(GameObject element)
    {
        if (GetVisibleElement() == element)
        {
            ScrollDown();
        }

        if (elements.Contains(element))
        {
            elements.Remove(element);
        }
    }

    GameObject GetVisibleElement()
    {
        if (elements.Count == 0)
            return null;

        return elements[scrollState];
    }

    void ScrollUp()
    {
        if (scrollState >= elements.Count - 1)
            return;
        
        GameObject visibleElement = GetVisibleElement();

        scrollState += 1;

        if (visibleElement != null)
        {
            float scrollDistance = GetScrollDistance(visibleElement) + spacing;
            StartCoroutine(MoveAllElements(scrollDistance));
        }
    }

    void ScrollDown()
    {
        if (scrollState <= 0)
            return;
        
        scrollState -= 1;

        GameObject visibleElement = GetVisibleElement();
        if (visibleElement != null)
        {
            float scrollDistance = GetScrollDistance(visibleElement) + spacing;
            StartCoroutine(MoveAllElements(-scrollDistance));
        }
    }

    IEnumerator MoveAllElements(float distance, float time = 0.25f)
    {
        // foreach (GameObject element in elements)
        // {
        //     if (element.GetComponent<Transform>() != null)
        //     {
        //         element.GetComponent<Transform>().position += new Vector3(0, distance, 0);
        //     }
        //     else if (element.GetComponent<RectTransform>() != null)
        //     {
        //         element.GetComponent<RectTransform>().position += new Vector3(0, WorldToUIScale(distance), 0);
        //     }
        // }
        

        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            float usedDeltaTime = Time.deltaTime;
            if (elapsedTime + usedDeltaTime > time)
            {
                usedDeltaTime = time - elapsedTime;
            }

            foreach (GameObject element in elements)
            {
                if (IsGraphElement(element))
                {
                    element.GetComponent<Transform>().position += new Vector3(0, WorldToUIScale(distance * usedDeltaTime / time), 0);
                }
                else if (IsWorldElement(element))
                {
                    element.GetComponent<Transform>().position += new Vector3(0, distance * usedDeltaTime / time, 0);
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    float GetUIElementHeight(GameObject element)
    {

        return element.GetComponent<RectTransform>().rect.height;
    }

    float GetWorldElementHeight(GameObject element)
    {
        return element.GetComponent<Transform>().localScale.y;
    }

    float GetWorldElementBottomY(GameObject element)
    {
        Transform transform = element.GetComponent<Transform>();
        return transform.position.y - transform.localScale.y / 2f;
    }

    float GetUIElementBottomY(GameObject element)
    {
        RectTransform rectTransform = element.GetComponent<RectTransform>();
        return rectTransform.position.y - UIToWorldScale(rectTransform.rect.height) / 2f;
    }

    float GetScrollDistance(GameObject element)
    {
        float height = 0f;
        if (IsGraphElement(element))
        {
            height = UIToWorldScale(element.GetComponent<GraphManager>().axes.rect.height);
        }
        if (IsWorldElement(element))
        {
            height = element.GetComponent<Transform>().localScale.y;
        }
        return height;
    }

    float UIToWorldScale(float uiScale)
    {
        return uiScale / (canvas.scaleFactor * scaleFactor);
    }

    float WorldToUIScale(float worldScale)
    {
        return worldScale * (canvas.scaleFactor * scaleFactor);
    }

    bool IsUIElement(GameObject element)
    {
        return element.GetComponent<RectTransform>() != null;
    }

    bool IsWorldElement(GameObject element)
    {
        return element.GetComponent<Transform>() != null && element.GetComponent<RectTransform>() == null;
    }

    bool IsGraphElement(GameObject element)
    {
        return element.GetComponent<GraphManager>() != null;
    }
}
