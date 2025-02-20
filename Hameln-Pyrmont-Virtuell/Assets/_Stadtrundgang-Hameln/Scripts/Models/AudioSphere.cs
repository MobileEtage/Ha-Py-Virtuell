using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSphere : MonoBehaviour
{
    public string audioFile = "";
    public string audioTitle = "";

    [Space(10)]

    public GameObject mainSpehere;
    public float radius = 0.35f;
    public float moveTime = 4.0f;
    public float targetScale = 0.4f;

    void Start()
    {
        targetScale = AudiothekController.instance.sphereDefaultScale;
    }

    void OnEnable()
    {
        StartCoroutine(AnimateCoroutine());
    }

    void Update()
    {
        mainSpehere.transform.localScale = Vector3.Lerp(mainSpehere.transform.localScale, Vector3.one*targetScale, Time.deltaTime*AudiothekController.instance.speechScaleLerpFactor);
    }

    public IEnumerator AnimateCoroutine()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        Vector3 pos = Random.insideUnitSphere*radius;

        if (AnimationController.instance != null)
        {
            yield return StartCoroutine(AnimationController.instance.AnimateMove3DCoroutine(mainSpehere.transform, mainSpehere.transform.localPosition, pos, moveTime, "smooth"));
            StartCoroutine(AnimateCoroutine());
        }
    }
}
