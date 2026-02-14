using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class InteractiveAnimator : MonoBehaviour
{
    [Header("Settings")]
    public float animationSpeed = 1f;
    
    [Header("Particles")]
    public GameObject electron1;
    [Space]
    public GameObject neon1;
    public GameObject neon2;
    public GameObject neon3;
    public GameObject neon4;
    [Space]
    public float burstDuration = 0.5f;
    public List<Texture2D> burstFrames;

    [Header("Prefabs")]
    public Transform burstsParent;
    public Transform particlesParent;
    public GameObject electronPrefab;
    public GameObject burstPrefab;
    public Texture2D neonTexture;
    public Texture2D neonIonTexture;

    private List<GameObject> addedObjects = new List<GameObject>();
    private Vector2 electron1StartPos;
    private Vector2 neon1StartPos;
    private Vector2 neon2StartPos;
    private Vector2 neon3StartPos;
    private Vector2 neon4StartPos;

    private float initialAnimationSpeed;

    void Start()
    {
        electron1StartPos = electron1.transform.localPosition;
        neon1StartPos = neon1.transform.localPosition;
        neon2StartPos = neon2.transform.localPosition;
        neon3StartPos = neon3.transform.localPosition;
        neon4StartPos = neon4.transform.localPosition;

        initialAnimationSpeed = animationSpeed;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetAnimation();
            Animate();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAnimation();
        }
    }

    public void AnimationSpeedToggled()
    {
        if (animationSpeed == initialAnimationSpeed)
            animationSpeed /= 3f;
        else
            animationSpeed = initialAnimationSpeed;
    }

    public void ResetAnimation()
    {
        StopAllCoroutines();

        foreach (GameObject obj in addedObjects)
        {
            Destroy(obj);
        }
        addedObjects.Clear();

        electron1.transform.localPosition = electron1StartPos;
        neon1.transform.localPosition = neon1StartPos;
        neon2.transform.localPosition = neon2StartPos;
        neon3.transform.localPosition = neon3StartPos;
        neon4.transform.localPosition = neon4StartPos;

        AssignTexture(neon1, neonTexture);
        AssignTexture(neon2, neonTexture);
        AssignTexture(neon3, neonTexture);
        AssignTexture(neon4, neonTexture);
    }

    public void Animate()
    {
        StartCoroutine(AnimateSequence());
    }

    public IEnumerator AnimateSequence()
    {
        StartCoroutine(AnimateVibration(neon1, amplitude: 0.05f, frequency: 3f, duration: 100000f / animationSpeed));
        StartCoroutine(AnimateVibration(neon2, amplitude: 0.05f, frequency: 2.8f, duration: 100000f / animationSpeed));
        StartCoroutine(AnimateVibration(neon3, amplitude: 0.05f, frequency: 3.2f, duration: 100000f / animationSpeed));
        StartCoroutine(AnimateVibration(neon4, amplitude: 0.05f, frequency: 3.3f, duration: 100000f / animationSpeed));

        yield return AnimateMovement(electron1, new Vector2(11.3f, 0f), 15f * animationSpeed);
        yield return AnimateMovement(electron1, new Vector2(1.15f, 2.6f), 15f * animationSpeed);

        AssignTexture(neon2, neonIonTexture);
        GameObject electron2 = AddElectron(electron1.transform.localPosition);
        StartCoroutine(AnimateBurst(electron1.transform.localPosition));
        StartCoroutine(AnimateMovement(electron1, new Vector2(9.1f, 0.1f), 10f * animationSpeed));
        yield return AnimateMovement(electron2, new Vector2(3.9f, -1.2f), 10f * animationSpeed);

        AssignTexture(neon3, neonIonTexture);
        GameObject electron3 = AddElectron(electron2.transform.localPosition);
        StartCoroutine(AnimateBurst(electron2.transform.localPosition));
        StartCoroutine(AnimateMovement(electron2, new Vector2(5.2f, -2.25f), 7.5f * animationSpeed));
        yield return AnimateMovement(electron3, new Vector2(1.3f, -2.35f), 7.5f * animationSpeed);

        AssignTexture(neon4, neonIonTexture);
        GameObject electron4 = AddElectron(electron3.transform.localPosition);
        StartCoroutine(AnimateBurst(electron3.transform.localPosition));
        StartCoroutine(AnimateMovement(electron3, new Vector2(3.9f, 1.25f), 5f * animationSpeed));
        yield return AnimateMovement(electron4, new Vector2(3.9f, -1.25f), 5f * animationSpeed);
    }

    public IEnumerator AnimateMovement(GameObject particle, Vector2 movement, float speed, float delay=0f)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        
        Vector2 start = particle.transform.localPosition;
        Vector2 end = start + movement;

        float duration = Vector2.Distance(start, end) / speed;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            particle.transform.localPosition = Vector2.Lerp(start, end, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        particle.transform.localPosition = end;
    }

    public IEnumerator AnimateVibration(GameObject particle, float amplitude, float frequency, float duration)
    {
        Vector2 originalPos = particle.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float offsetX = Mathf.Sin(elapsed * frequency * 2f * Mathf.PI) * amplitude;
            float offsetY = Mathf.Sin((elapsed + 0.25f) * frequency * 2f * Mathf.PI * 1.5f) * amplitude;
            particle.transform.localPosition = originalPos + new Vector2(offsetX, offsetY);
            elapsed += Time.deltaTime;
            yield return null;
        }
        particle.transform.localPosition = originalPos;
    }

    public IEnumerator AnimateBurst(Vector2 position)
    {
        GameObject burst = Instantiate(burstPrefab, burstsParent);
        burst.SetActive(true);
        burst.transform.localPosition = position;
        addedObjects.Add(burst);

        for (int i = 0; i < burstFrames.Count; i++)
        {
            AssignTexture(burst, burstFrames[i]);
            yield return new WaitForSeconds(burstDuration / burstFrames.Count / animationSpeed);
        }

        Destroy(burst);
        addedObjects.Remove(burst);
    }

    public GameObject AddElectron(Vector2 position)
    {
        GameObject electron = Instantiate(electronPrefab, particlesParent);
        electron.SetActive(true);
        electron.transform.localPosition = position;
        addedObjects.Add(electron);
        return electron;
    }

    public Vector2 CollisionLerp(Vector2 start, Vector2 end, float time)
    {
        time = Mathf.Clamp01(time);
        return Vector2.Lerp(start, end, Mathf.Sqrt(time));
    }

    public void AssignTexture(GameObject target, Texture2D texture)
    {
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }
    }
}
