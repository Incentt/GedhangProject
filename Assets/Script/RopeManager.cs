using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RopeManager : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    public GameObject player1;
    public GameObject player2;

    public GameObject ropeSegmentPrefab;

    public int ropeSegmentCountHalved = 10;
    public float distancePerSegment = 0.1f;
    private float DistancePerSegment
    {
        get { return distancePerSegment; }
        set
        {
            distancePerSegment = value;
            UpdateRopeDistancePerSegments();
        }
    }

    public float ropeLengthDefault = 4f;
    private float _ropeLength;
    private float RopeLength
    {
        get { return _ropeLength; }
        set
        {
            _ropeLength = value;
            int segmentCount = GetTotalRopeSegmentCount();
            DistancePerSegment = _ropeLength / segmentCount;
            UpdateSpringDistance(_ropeLength);
            print("Rope Length Updated: " + _ropeLength); // debug
        }
    }


    private List<GameObject> _ropeSegments = new List<GameObject>();

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        CreateRope();
    }

    private void Start()
    {
        RopeLength = ropeLengthDefault;
    }


    private void Update()
    {
        DrawRope();
    }

    private void CreateRope()
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

        _ropeSegments.Add(ropeSegmentMid);

        foreach (GameObject objTarget in new GameObject[] { player1, player2 })
        {

            for (int i = 1; i <= ropeSegmentCountHalved; i++)
            {
                Vector3 position = Vector3.Lerp(midPoint, objTarget.transform.position, (float)i / ropeSegmentCountHalved);
                GameObject ropeSegment = Instantiate(ropeSegmentPrefab, position, Quaternion.identity);
                ropeSegment.name = "RopeSegment_" + objTarget.name + "_" + i;
                _ropeSegments.Add(ropeSegment);


                ropeSegment.GetComponent<DistanceJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 2].GetComponent<Rigidbody2D>();

            }

            objTarget.GetComponent<DistanceJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 1].GetComponent<Rigidbody2D>();

            if (objTarget == player1)
            {
                _ropeSegments.Reverse();
            }
        }
    }

    private void DrawRope()
    {
        if (_lineRenderer == null)
        {
            Debug.LogError("LineRenderer component is not assigned.");
            return;
        }

        _lineRenderer.positionCount = _ropeSegments.Count() + 2;

        _lineRenderer.SetPosition(0, player1.transform.position);
        int counter = 1;
        foreach (GameObject ropeSegment in _ropeSegments)
        {
            Vector3 position = ropeSegment.transform.position;
            _lineRenderer.SetPosition(counter, position);
            counter++;
        }
        _lineRenderer.SetPosition(counter, player2.transform.position);


    }

    private void UpdateRopeDistancePerSegments()
    {
        foreach (GameObject ropeSegment in _ropeSegments)
        {
            if (ropeSegment.GetComponent<DistanceJoint2D>() != null)
            {
                ropeSegment.GetComponent<DistanceJoint2D>().distance = distancePerSegment;
            }
        }

        foreach (GameObject objTarget in new GameObject[] { player1, player2 })
        {
            objTarget.GetComponent<DistanceJoint2D>().distance = distancePerSegment;
        }
    }

    private int GetTotalRopeSegmentCount()
    {
        int segmentCount = _ropeSegments.Count - 1 + 2; // Exclude the mid segment, plus the two end objects
        return segmentCount;
    }

    private void UpdateSpringDistance(float distance)
    {
        if (player1.GetComponent<SpringJoint2D>() == null)
        {
            Debug.LogError("First object must have a SpringJoint2D connected to the second object.");
            return;
        }

        player1.GetComponent<SpringJoint2D>().distance = distance;
    }
}