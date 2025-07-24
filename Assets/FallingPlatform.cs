using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    public float shakeMagnitude = 0.1f;
    public float shakeTime = 1.0f;
    public float respawnDelay = 2.0f;
    private Transform platformTransform;

    private Vector3 originalPosition;
    private bool isShaking = false;
    private bool isFalling = false;

    void Start()
    {
        if (platformTransform == null)
            platformTransform = transform;
        originalPosition = platformTransform.position;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isShaking && !isFalling && collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(ShakeAndFall());
        }
    }

    IEnumerator ShakeAndFall()
    {
        isShaking = true;
        float elapsed = 0.0f;

        while (elapsed < shakeTime)
        {
            Vector3 randomPoint = originalPosition + (Vector3)Random.insideUnitCircle * shakeMagnitude;
            platformTransform.position = randomPoint;
            elapsed += Time.deltaTime;
            yield return null;
        }

        platformTransform.position = originalPosition;
        isShaking = false;
        isFalling = true;

        yield return new WaitForSeconds(shakeTime / 100);

        // Disable SpriteRenderer and Collider2D
        var sr = platformTransform.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        var col = platformTransform.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        // Re-enable SpriteRenderer and Collider2D
        platformTransform.position = originalPosition;
        if (sr != null) sr.enabled = true;
        if (col != null) col.enabled = true;
        isFalling = false;
    }
}
