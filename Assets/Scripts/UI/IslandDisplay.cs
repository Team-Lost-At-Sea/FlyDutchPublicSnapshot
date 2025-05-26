using System.Collections;
using TMPro;
using UnityEngine;
public class IslandDisplay : MonoBehaviour
{
    public TextMeshProUGUI islandNameText;
    public float displayDuration = 2f;
    public float fadeDuration = 1f;

    private Coroutine displayCoroutine;

    public void ShowIslandName(string islandName)
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        displayCoroutine = StartCoroutine(DisplayIslandNameCoroutine(islandName));
    }

    private IEnumerator DisplayIslandNameCoroutine(string islandName)
    {
        Color baseColor = islandNameText.color;
        baseColor.a = 0f;
        islandNameText.color = baseColor;
        islandNameText.text = islandName;

        // Fade in
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            islandNameText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        islandNameText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

        // Wait on screen
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Clamp01(1 - (elapsedTime / fadeDuration));
            islandNameText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        islandNameText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
    }
}
