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

    public GameObject ropeSegmentPrefab;

    public int ropeSegmentCountHalved = 10;
    public float ropeLength = 4f;
    private float _distancePerSegment;

    // private float RopeLength
    // {
    //     get { return _ropeLength; }
    //     set
    //     {
    //         _ropeLength = value;
    //         int segmentCount = GetTotalRopeSegmentCount();
    //         distancePerSegment = _ropeLength / segmentCount;
    //         // UpdateSpringDistance(_ropeLength);
    //         print("Rope Length Updated: " + _ropeLength); // debug
    //     }
    // }


    private List<GameObject> _ropeSegments = new List<GameObject>();

    public void InitializeRope()
    {
        InitializePlayer();
        CreateRope();
        SetRopeLength(ropeLength);
    }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        // RopeLength = ropeLengthDefault;
    }


    private void Update()
    {
        DrawRope();
        // SetRopeLength(ropeLength);
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
            distanceJoint2D.maxDistanceOnly = true;
            distanceJoint2D.autoConfigureDistance = false;
        }
        if (player2.GetComponent<DistanceJoint2D>() == null)
        {
            DistanceJoint2D distanceJoint2D = player2.AddComponent<DistanceJoint2D>();
            distanceJoint2D.maxDistanceOnly = true;
            distanceJoint2D.autoConfigureDistance = false;
        }

        // if (player1.GetComponent<SpringJoint2D>() == null)
        // {
        //     SpringJoint2D springJoint2D = player1.AddComponent<SpringJoint2D>();
        //     springJoint2D.enableCollision = true;
        // }
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
        ropeSegmentMid.transform.parent = transform; // Set parent to RopeManager
        _ropeSegments.Add(ropeSegmentMid);

        foreach (GameObject objTarget in new GameObject[] { player1, player2 })
        {

            for (int i = 1; i <= ropeSegmentCountHalved; i++)
            {
                Vector3 position = Vector3.Lerp(midPoint, objTarget.transform.position, (float)i / ropeSegmentCountHalved);
                GameObject ropeSegment = Instantiate(ropeSegmentPrefab, position, Quaternion.identity);
                ropeSegment.name = "RopeSegment_" + objTarget.name + "_" + i;
                _ropeSegments.Add(ropeSegment);
                ropeSegment.transform.parent = transform; // Set parent to RopeManager

                ropeSegment.GetComponent<DistanceJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 2].GetComponent<Rigidbody2D>();
                ropeSegment.GetComponent<DistanceJoint2D>().maxDistanceOnly = true;
                ropeSegment.GetComponent<DistanceJoint2D>().autoConfigureDistance = false;
            }

            objTarget.GetComponent<DistanceJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 1].GetComponent<Rigidbody2D>();

            if (objTarget == player1)
            {
                _ropeSegments.Reverse();
            }
        }
        StartCoroutine(EnablePlayer2JointDelayed(player2.GetComponent<DistanceJoint2D>()));
        StartCoroutine(EnablePlayer2JointDelayed(player1.GetComponent<DistanceJoint2D>()));

    }

    private void DrawRope()
    {
        if (_lineRenderer == null || player1 == null || player2 == null || _ropeSegments.Count == 0)
        {
            return;
        }

        int totalPoints = GetTotalRopeSegmentCount();
        _lineRenderer.positionCount = totalPoints;

        // Use Vector3.Lerp for smoothing
        Vector3 prevPos = _lineRenderer.GetPosition(0);
        Vector3 targetPos = player1.transform.position;
        _lineRenderer.SetPosition(0, Vector3.Lerp(prevPos, targetPos, 0.5f));

        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            prevPos = _lineRenderer.GetPosition(i + 1);
            targetPos = _ropeSegments[i].transform.position;
            _lineRenderer.SetPosition(i + 1, Vector3.Lerp(prevPos, targetPos, 0.5f));
        }

        prevPos = _lineRenderer.GetPosition(totalPoints - 1);
        targetPos = player2.transform.position;
        _lineRenderer.SetPosition(totalPoints - 1, Vector3.Lerp(prevPos, targetPos, 0.5f));
    }

    private int GetTotalRopeSegmentCount()
    {
        int segmentCount = _ropeSegments.Count - 1 + 2; // Exclude the mid segment, plus the two end objects
        return segmentCount;
    }

    public void SetRopeLength(float value)
    {
        if (player1 == null || player2 == null || _ropeSegments.Count == 0)
        {
            return;
        }

        // Update the rope length
        int segmentCount = GetTotalRopeSegmentCount();
        _distancePerSegment = ropeLength / segmentCount;
        player1.GetComponent<DistanceJoint2D>().distance = _distancePerSegment;
        player2.GetComponent<DistanceJoint2D>().distance = _distancePerSegment;
        foreach (GameObject segment in _ropeSegments)
        {
            if (segment != null && segment.GetComponent<DistanceJoint2D>() != null)
            {
                segment.GetComponent<DistanceJoint2D>().distance = _distancePerSegment;
            }
        }
    }

    // private void UpdateSpringDistance(float distance)
    // {
    //     if (player1.GetComponent<SpringJoint2D>() == null)
    //     {
    //         Debug.LogError("First object must have a SpringJoint2D connected to the second object.");
    //         return;
    //     }

    //     player1.GetComponent<SpringJoint2D>().distance = distance;
    // }

    private IEnumerator EnablePlayer2JointDelayed(DistanceJoint2D joint)
    {
        yield return new WaitForFixedUpdate();
        joint.enabled = false;
        yield return new WaitForFixedUpdate();
        joint.enabled = true;

        // Also reset the connected body
        Rigidbody2D connectedRb = joint.connectedBody;
        joint.connectedBody = null;
        yield return new WaitForFixedUpdate();
        joint.connectedBody = connectedRb;
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