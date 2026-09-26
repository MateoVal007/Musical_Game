using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class TutorialStep
{
    [TextArea] public string text;

    [Tooltip("Segundos desde que arranca el nivel hasta que aparece este texto.")]
    public float delay;

    [Tooltip("Cuánto tiempo queda visible antes de desvanecerse.")]
    public float duration = 4f;
}

public class TutorialController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private List<TutorialStep> steps;
    [SerializeField] private float fadeSpeed = 3f;

    void Start()
    {
        canvasGroup.alpha = 0f;
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        float elapsed = 0f;
        foreach (var step in steps)
        {
            float waitTime = step.delay - elapsed;
            if (waitTime > 0f) yield return new WaitForSeconds(waitTime);
            elapsed = step.delay;

            tutorialText.text = step.text;
            yield return Fade(1f);

            yield return new WaitForSeconds(step.duration);
            elapsed += step.duration;

            yield return Fade(0f);
        }
    }

    private IEnumerator Fade(float target)
    {
        while (!Mathf.Approximately(canvasGroup.alpha, target))
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
            yield return null;
        }
    }
}