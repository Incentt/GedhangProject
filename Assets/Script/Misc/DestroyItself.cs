using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// This script is responsible for destroying the GameObject after a specified time
// It can be used for temporary objects like particles, effects, or enemies that should disappear after initialization

public class DestroyItself : MonoBehaviour
{

    public float destroyTime = 3f;
    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}
