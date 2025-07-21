using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeManager : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    private GameObject player1;
    private GameObject player2;

    public int ropeSegmentCount = 13; // odd please

    private List<GameObject> _ropeSegments = new List<GameObject>();

    public void InitializeRope()
    {
        InitializePlayer();
        MakeConnection();
    }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        
    }


    private void Update()
    {
        UpdateAllLineRendererPointsPosition();
    }

    private void InitializePlayer()
    {
        if (player1 == null || player2 == null)
        {
            player1 = GameManager.Instance.currentPlayer1;
            player2 = GameManager.Instance.currentPlayer2;
        }

        if (player1.GetComponent<HingeJoint2D>() == null)
        {
            HingeJoint2D hingeJoint2D = player1.AddComponent<HingeJoint2D>();
        }

        if (player2.GetComponent<HingeJoint2D>() == null)
        {
            HingeJoint2D hingeJoint2D = player2.AddComponent<HingeJoint2D>();
        }
    }

    private void MakeConnection()
    {
        if (player1 == null || player2 == null)
        {
            Debug.LogError("First or second object is not assigned.");
            return;
        }

        // Get mid point
        Vector3 midPoint = Vector3.Lerp(player1.transform.position, player2.transform.position, 0.5f);
        GameObject ropeSegmentMid = new GameObject("RopeSegmentMid", typeof(Rigidbody2D));
        ropeSegmentMid.tag = "RopeSegment";
        ropeSegmentMid.transform.position = midPoint;
        ropeSegmentMid.transform.parent = transform; // Set parent to RopeManager
        _ropeSegments.Add(ropeSegmentMid);
        
        Vector2 startPosition = player1.transform.position;
        Vector2 endPosition = player2.transform.position;

        foreach (GameObject objTarget in new GameObject[] { player1, player2 })
        {
            for (int i = ropeSegmentCount / 2; i > 0; i--)
            {

                if (objTarget == player1)
                {
                    startPosition = player1.transform.position;
                    endPosition = player2.transform.position;
                }
                else
                {
                    startPosition = player2.transform.position;
                    endPosition = player1.transform.position;
                }

                Vector3 position = Vector3.Lerp(startPosition, endPosition, (float) i / ropeSegmentCount);
                GameObject ropeSegment = new GameObject("RopeSegment_" + objTarget.name + "_" + i, typeof(Rigidbody2D));
                ropeSegment.tag = "RopeSegment";
                ropeSegment.AddComponent<HingeJoint2D>();
                ropeSegment.transform.position = position;

                _ropeSegments.Add(ropeSegment);
                ropeSegment.transform.parent = transform; // Set parent to RopeManager

                ropeSegment.GetComponent<HingeJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 2].GetComponent<Rigidbody2D>();
            }

            objTarget.GetComponent<HingeJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 1].GetComponent<Rigidbody2D>();

            if (objTarget == player1)
            {
                _ropeSegments.Reverse(); // Reverse the order to connect from player1 to player2
            }
                
        }
    }

    private void UpdateAllLineRendererPointsPosition()
    {
        if (_lineRenderer == null || player1 == null || player2 == null || _ropeSegments.Count == 0)
        {
            return;
        }

        int totalPoints = ropeSegmentCount + 1;
        _lineRenderer.positionCount = totalPoints;

        // Use Vector3.Lerp for smoothing
        Vector3 prevPos = _lineRenderer.GetPosition(0);
        Vector3 targetPos = player1.transform.position;
        _lineRenderer.SetPosition(0, Vector3.Lerp(prevPos, targetPos, 0.5f));

        int segmentIndex = 1;
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            if (_ropeSegments[i].name.Contains("Mid"))
            {
                continue; // Skip mid segment
            }

            prevPos = _lineRenderer.GetPosition(segmentIndex);
            targetPos = _ropeSegments[i].transform.position;
            _lineRenderer.SetPosition(segmentIndex, Vector3.Lerp(prevPos, targetPos, 0.5f));
            segmentIndex++;
        }

        prevPos = _lineRenderer.GetPosition(totalPoints-1);
        targetPos = player2.transform.position;
        _lineRenderer.SetPosition(totalPoints-1, Vector3.Lerp(prevPos, targetPos, 0.5f));
    }
    public void DestroyRope()
    {
        foreach (GameObject segment in _ropeSegments)
        {
            if (segment != null)
            {
                Destroy(segment);
            }
        }

        _ropeSegments.Clear();
        _lineRenderer.positionCount = 0;
    }
}