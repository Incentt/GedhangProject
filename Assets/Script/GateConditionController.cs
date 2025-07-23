using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GateConditionController : MonoBehaviour
{
    public GameObject gate;
    public List<GameObject> conditions;

    void Update()
    {
        // Check every second
        if (Time.frameCount % Mathf.RoundToInt(1f / Time.deltaTime) == 0)
        {
            foreach (var condition in conditions)
            {
                if (condition == null)
                {
                    GateAction();
                    break;
                }
            }
        }
    }
    void GateAction()
    {
        if (gate != null)
        {
            gate.SetActive(false);
        }
    }

}
