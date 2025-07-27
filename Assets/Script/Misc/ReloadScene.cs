using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadScene : MonoBehaviour
{
    private BoxCollider2D _boxCollider;
    private bool hasTriggered = false; // Prevent multiple triggers
    
    void Awake()
    {
        _boxCollider = GetComponent<BoxCollider2D>();
        if (_boxCollider != null)
        {
            _boxCollider.isTrigger = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return; // Prevent multiple calls
        if (other.CompareTag("Player"))
        {
            hasTriggered = true; // Set the flag to true
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
    
    // private IEnumerator ReloadAfterDelay()
    // {
    //     // Wait a frame to let current physics/scripts finish
    //     yield return new WaitForEndOfFrame();
        
    //     Debug.Log("Reloading scene...");
    //     SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    // }
}