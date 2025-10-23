using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapeProjective : MonoBehaviour
{
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float heightY = 3f;
    [SerializeField] private GameObject groupProjectileShadow;
    [SerializeField] private GameObject landPrefab;

    private void Start()
    {
        GameObject grapeShadow = Instantiate(groupProjectileShadow, transform.position + new Vector3(0, -0.3f, 0), Quaternion.identity);
        Vector3 PlayerPos = PlayerController.Instance.transform.position;
        Vector3 grapeShadowStartPos = grapeShadow.transform.position;

        StartCoroutine(ProjectileCurveRoutine(transform.position, PlayerPos));
        StartCoroutine(MoveGrapeShadowRoutine(grapeShadow, grapeShadowStartPos, PlayerPos));
    }

    private IEnumerator ProjectileCurveRoutine(Vector3 startPos, Vector3 endPos)
    {
        float timePassed = 0f;
        while (timePassed < duration)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / duration;
            float heightT = animCurve.Evaluate(linearT);
            float height = Mathf.Lerp(0f, heightY, heightT);

            transform.position = Vector2.Lerp(startPos, endPos, linearT) + new Vector2(0f, height);
            yield return null;
        }
        Instantiate(landPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
    private IEnumerator MoveGrapeShadowRoutine(GameObject grapeShadow, Vector3 startPos, Vector3 endPos)
    {
        float timePassed = 0f;
        while(timePassed < duration)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / duration;
            grapeShadow.transform.position = Vector2.Lerp(startPos, endPos, linearT);
            yield return null;  

        }
        Destroy(grapeShadow);
    }
}
