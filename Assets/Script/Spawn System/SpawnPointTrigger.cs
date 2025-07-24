using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpawnPointTrigger : MonoBehaviour
{
    [Header("Spawn Point Settings")]
    [SerializeField] private int spawnPointIndex;
    [SerializeField] private bool requireBothPlayers = false; // If true, both players must be in trigger to activate
    [SerializeField] private bool activateOnce = true; // If true, can only be activated once

    [Header("Visual Feedback")]
    [SerializeField] private bool showActivationEffect = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationSound;

    private SpawnPointController spawnController;
    private bool isActivated = false;
    private bool player1InTrigger = false;
    private bool player2InTrigger = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D triggerCollider;


    private void Start()
    {
        // Get components
        triggerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Ensure collider is set as trigger
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        // Find spawn controller
        spawnController = FindObjectOfType<SpawnPointController>();
        // Set initial visual state
        UpdateVisualState();

        // Auto-setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && activationSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
        //assign spawn point index from controller 
        if (spawnController != null && spawnController.spawnPoints != null)
        {
            int index = -1;
            for (int i = 0; i < spawnController.spawnPoints.Length; i++)
            {
                if (spawnController.spawnPoints[i] == this.transform)
                {
                    index = i;
                    break;
                }
            }
            if (index >= 0)
            {
                spawnPointIndex = index;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;
        if (player.PlayerType == PlayerType.Player1)
        {
            player1InTrigger = true;
        }
        else if (player.PlayerType == PlayerType.Player2)
        {
            player2InTrigger = true;
        }
        CheckForActivation();
    }

    private void CheckForActivation()
    {
        if (isActivated && activateOnce) return;

        bool shouldActivate = false;

        shouldActivate = player1InTrigger || player2InTrigger;


        if (shouldActivate)
        {
            ActivateSpawnPoint();
        }

        // Update visual state regardless
        UpdateVisualState();
    }

    private void ActivateSpawnPoint()
    {
        if (spawnController == null)
        {
            Debug.LogError($"Cannot activate spawn point: SpawnPointController not found!");
            return;
        }

        // Set this as the current spawn point
        spawnController.SetCurrentSpawnPoint(spawnPointIndex);
        isActivated = true;

        // Play activation sound
        if (audioSource != null && activationSound != null)
        {
            //audioSource.PlayOneShot(activationSound);
        }

        // Update visual state
        UpdateVisualState();


        // Optional: Add particle effect or other feedback here
        if (showActivationEffect)
        {
            StartCoroutine(ActivationEffect());
        }
    }

    private void UpdateVisualState()
    {
        if (spriteRenderer == null) return;

        if (isActivated)
        {

        }
        else if (player1InTrigger || player2InTrigger)
        {

        }
        else
        {

        }
    }

    private IEnumerator ActivationEffect()
    {
        // Simple color pulse effect
        Color originalColor = spriteRenderer.color;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, Color.white, t);
            yield return null;
        }

        spriteRenderer.color = originalColor;
    }

}
