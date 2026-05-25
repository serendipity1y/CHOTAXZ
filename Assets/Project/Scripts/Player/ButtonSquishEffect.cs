using UnityEngine;
using System.Collections;

public class ButtonSquishEffect : MonoBehaviour
{
    [SerializeField] private float squishScale = 0.7f;
    [SerializeField] private float squishDuration = 0.1f;
    [SerializeField] private float returnDuration = 0.15f;

    private Vector3 originalScale;
    private Coroutine squishCoroutine;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    public void Squish()
    {
        if (squishCoroutine != null)
            StopCoroutine(squishCoroutine);

        Debug.Log("Squished");
        squishCoroutine = StartCoroutine(SquishCoroutine());
    }

    private IEnumerator SquishCoroutine()
    {
        // Squish down
        float elapsed = 0f;
        while (elapsed < squishDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / squishDuration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * squishScale, t);
            yield return null;
        }

        // Return to original
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            transform.localScale = Vector3.Lerp(originalScale * squishScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
