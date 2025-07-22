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

    // public int ropeSegmentCount = 13; // odd please
    public int ropeSegmentCountHalved = 2; // odd please

    public float ropeLength = 6f;

    private List<GameObject> _ropeSegments = new List<GameObject>();
    private float _distanceEachSegment = 2f;
    private DistanceJoint2D _directJoint;
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
        SetRopeLength(ropeLength);
    }


    private void Update()
    {
        UpdateAllLineRendererPointsPosition();
        SetRopeLength(ropeLength);
    }

    private void InitializePlayer()
    {
        if (player1 == null || player2 == null)
        {
            player1 = GameManager.Instance.currentPlayer1;
            player2 = GameManager.Instance.currentPlayer2;
        }

        if (player1.GetComponent<DistanceJoint2D>() == null)
        {
            DistanceJoint2D distanceJoint2D = player1.AddComponent<DistanceJoint2D>();
            distanceJoint2D.autoConfigureDistance = false;
            distanceJoint2D.maxDistanceOnly = true;
        }

        if (player2.GetComponent<DistanceJoint2D>() == null)
        {
            DistanceJoint2D distanceJoint2D = player2.AddComponent<DistanceJoint2D>();
            distanceJoint2D.autoConfigureDistance = false;
            distanceJoint2D.maxDistanceOnly = true;
        }

        _directJoint = player1.AddComponent<DistanceJoint2D>();
        _directJoint.autoConfigureDistance = false;
        _directJoint.maxDistanceOnly = true;
        _directJoint.connectedBody = player2.GetComponent<Rigidbody2D>();
        _directJoint.enableCollision = true;
    }

    // HingeJoint2D
    // private void MakeConnection()
    // {
    //     if (player1 == null || player2 == null)
    //     {
    //         Debug.LogError("First or second object is not assigned.");
    //         return;
    //     }

    //     // Get mid point
    //     Vector3 midPoint = Vector3.Lerp(player1.transform.position, player2.transform.position, 0.5f);
    //     GameObject ropeSegmentMid = new GameObject("RopeSegmentMid", typeof(Rigidbody2D));
    //     ropeSegmentMid.tag = "RopeSegment";
    //     ropeSegmentMid.transform.position = midPoint;
    //     ropeSegmentMid.transform.parent = transform; // Set parent to RopeManager
    //     _ropeSegments.Add(ropeSegmentMid);

    //     Vector2 startPosition = player1.transform.position;
    //     Vector2 endPosition = player2.transform.position;

    //     foreach (GameObject objTarget in new GameObject[] { player1, player2 })
    //     {
    //         for (int i = ropeSegmentCount / 2; i > 0; i--)
    //         {

    //             if (objTarget == player1)
    //             {
    //                 startPosition = player1.transform.position;
    //                 endPosition = player2.transform.position;
    //             }
    //             else
    //             {
    //                 startPosition = player2.transform.position;
    //                 endPosition = player1.transform.position;
    //             }

    //             Vector3 position = Vector3.Lerp(startPosition, endPosition, (float) i / ropeSegmentCount);
    //             GameObject ropeSegment = new GameObject("RopeSegment_" + objTarget.name + "_" + i, typeof(Rigidbody2D));
    //             ropeSegment.tag = "RopeSegment";
    //             ropeSegment.AddComponent<HingeJoint2D>();
    //             ropeSegment.transform.position = position;

    //             _ropeSegments.Add(ropeSegment);
    //             ropeSegment.transform.parent = transform; // Set parent to RopeManager

    //             ropeSegment.GetComponent<HingeJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 2].GetComponent<Rigidbody2D>();
    //         }

    //         objTarget.GetComponent<HingeJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 1].GetComponent<Rigidbody2D>();

    //         if (objTarget == player1)
    //         {
    //             _ropeSegments.Reverse(); // Reverse the order to connect from player1 to player2
    //         }

    //     }
    // }
    
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

        foreach (GameObject objTarget in new GameObject[] { player1, player2 })
        {
            for (int i = 1; i < ropeSegmentCountHalved; i++)
            {

                Vector3 position = Vector3.Lerp(midPoint, objTarget.transform.position, (float)i / ropeSegmentCountHalved);
                GameObject ropeSegment = new GameObject("RopeSegment_" + objTarget.name + "_" + i, typeof(Rigidbody2D));
                ropeSegment.tag = "RopeSegment";
                ropeSegment.AddComponent<DistanceJoint2D>();
                ropeSegment.transform.position = position;

                _ropeSegments.Add(ropeSegment);
                ropeSegment.transform.parent = transform; // Set parent to RopeManager

                ropeSegment.GetComponent<DistanceJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 2].GetComponent<Rigidbody2D>();
                ropeSegment.GetComponent<DistanceJoint2D>().autoConfigureDistance = false;
                ropeSegment.GetComponent<DistanceJoint2D>().maxDistanceOnly = true;
            }

            objTarget.GetComponent<DistanceJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 1].GetComponent<Rigidbody2D>();

            if (objTarget == player1)
            {
                _ropeSegments.Reverse(); // Reverse the order to connect from player1 to player2
            }
                
        }
    }

    private int GetTotalSegmentPoints()
    {
        return ropeSegmentCountHalved * 2 + 1;
    }

    private void UpdateAllLineRendererPointsPosition()
    {
        if (_lineRenderer == null || player1 == null || player2 == null || _ropeSegments.Count == 0)
        {
            return;
        }

        int totalPoints = GetTotalSegmentPoints();
        _lineRenderer.positionCount = totalPoints;

        // Use Vector3.Lerp for smoothing
        Vector3 prevPos = _lineRenderer.GetPosition(0);
        Vector3 targetPos = player1.transform.position;
        _lineRenderer.SetPosition(0, Vector3.Lerp(prevPos, targetPos, 0.5f));

        int segmentIndex = 1;
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            prevPos = _lineRenderer.GetPosition(segmentIndex);
            targetPos = _ropeSegments[i].transform.position;
            _lineRenderer.SetPosition(segmentIndex, Vector3.Lerp(prevPos, targetPos, 0.5f));
            segmentIndex++;
        }

        prevPos = _lineRenderer.GetPosition(totalPoints - 1);
        targetPos = player2.transform.position;
        _lineRenderer.SetPosition(totalPoints - 1, Vector3.Lerp(prevPos, targetPos, 0.5f));
    }

    public void SetRopeLength(float length)
    {
        _distanceEachSegment = length / (ropeSegmentCountHalved * 2);

        // Update the distance for each segment
        foreach (GameObject segment in _ropeSegments)
        {
            if (segment != null && segment.GetComponent<DistanceJoint2D>() != null)
            {
                segment.GetComponent<DistanceJoint2D>().distance = _distanceEachSegment;
            }
        }

        if (player1.GetComponent<DistanceJoint2D>() != null)
        {
            DistanceJoint2D distanceJoint2D = player1.GetComponent<DistanceJoint2D>();
            distanceJoint2D.distance = _distanceEachSegment;
        }

        if (player2.GetComponent<DistanceJoint2D>() != null)
        {
            DistanceJoint2D distanceJoint2D = player2.GetComponent<DistanceJoint2D>();
            distanceJoint2D.distance = _distanceEachSegment;
        }

        _directJoint.distance = length;

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