using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GraphScrollManager : MonoBehaviour
{
    public GameObject graphParent;

    [Space]
    public List<GameObject> graphs = new List<GameObject>();

    [Space]
    public int scrollIndex;

    void Start()
    {
        foreach (Transform child in graphParent.transform)
        {
            graphs.Add(child.gameObject);
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ScrollLeft();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ScrollRight();
        }
    }

    public void ScrollLeft(float time = 0.25f)
    {
        if (scrollIndex >= graphs.Count - 1)
            return;

        scrollIndex++;
        StartCoroutine(ScrollGraphs(-GetMoveDistance(), time));
    }

    public void ScrollRight(float time = 0.25f)
    {
        if (scrollIndex <= 0)
            return;

        scrollIndex--;
        StartCoroutine(ScrollGraphs(GetMoveDistance(), time));
    }

    IEnumerator ScrollGraphs(float distance, float time = 0.25f)
    {
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            foreach (GameObject graph in graphs)
            {
                if (graph != null)
                {
                    graph.transform.position += new Vector3(distance * Time.deltaTime / time, 0f, 0f);
                }
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    float GetMoveDistance()
    {
        if (graphs.Count <= 1)
        {
            return 0f;
        }
        else
        {
            return graphs[1].transform.position.x - graphs[0].transform.position.x;
        }
    }
}
