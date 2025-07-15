using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RopeManager : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    public GameObject _firstObject;
    public GameObject _secondObject;

    public GameObject _ropeSegmentPrefab;

    private int _ropeSegmentCountHalved = 2;
    private float _distancePerSegment = 0.1f;
    private float DistancePerSegment
    {
        get { return _distancePerSegment; }
        set
        {
            _distancePerSegment = value;
            UpdateRopeDistancePerSegments();
        }
    }

    public float _ropeLengthDefault = 4f;
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
        RopeLength = _ropeLengthDefault;
    }


    private void Update()
    {
        DrawRope();
    }

    private void CreateRope()
    {
        if (_firstObject == null || _secondObject == null)
        {
            Debug.LogError("First or second object is not assigned.");
            return;
        }

        // Get mid point
        Vector3 midPoint = Vector3.Lerp(_firstObject.transform.position, _secondObject.transform.position, 0.5f);
        GameObject ropeSegmentMid = new GameObject("RopeSegmentMid", typeof(Rigidbody2D));
        ropeSegmentMid.tag = "RopeSegment";
        ropeSegmentMid.transform.position = midPoint;

        _ropeSegments.Add(ropeSegmentMid);

        foreach (GameObject objTarget in new GameObject[] { _firstObject, _secondObject })
        {

            for (int i = 1; i <= _ropeSegmentCountHalved; i++)
            {
                Vector3 position = Vector3.Lerp(midPoint, objTarget.transform.position, (float)i / _ropeSegmentCountHalved);
                GameObject ropeSegment = Instantiate(_ropeSegmentPrefab, position, Quaternion.identity);
                ropeSegment.name = "RopeSegment_" + objTarget.name + "_" + i;
                _ropeSegments.Add(ropeSegment);


                ropeSegment.GetComponent<DistanceJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 2].GetComponent<Rigidbody2D>();

            }

            objTarget.GetComponent<DistanceJoint2D>().connectedBody = _ropeSegments[_ropeSegments.Count - 1].GetComponent<Rigidbody2D>();

            if (objTarget == _firstObject)
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

        _lineRenderer.SetPosition(0, _firstObject.transform.position);
        int counter = 1;
        foreach (GameObject ropeSegment in _ropeSegments)
        {
            Vector3 position = ropeSegment.transform.position;
            _lineRenderer.SetPosition(counter, position);
            counter++;
        }
        _lineRenderer.SetPosition(counter, _secondObject.transform.position);


    }

    private void UpdateRopeDistancePerSegments()
    {
        foreach (GameObject ropeSegment in _ropeSegments)
        {
            if (ropeSegment.GetComponent<DistanceJoint2D>() != null)
            {
                ropeSegment.GetComponent<DistanceJoint2D>().distance = _distancePerSegment;
            }
        }

        foreach (GameObject objTarget in new GameObject[] { _firstObject, _secondObject })
        {
            objTarget.GetComponent<DistanceJoint2D>().distance = _distancePerSegment;
        }
    }

    private int GetTotalRopeSegmentCount()
    {
        int segmentCount = _ropeSegments.Count - 1 + 2; // Exclude the mid segment, plus the two end objects
        return segmentCount;
    }

    private void UpdateSpringDistance(float distance)
    {
        if (_firstObject.GetComponent<SpringJoint2D>() == null)
        {
            Debug.LogError("First object must have a SpringJoint2D connected to the second object.");
            return;
        }

        _firstObject.GetComponent<SpringJoint2D>().distance = distance;
    }
}
