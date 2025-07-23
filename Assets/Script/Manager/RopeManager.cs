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
    // public int ropeSegmentCountHalved = 2; // odd please

    [SerializeField] private float _defaultRopeLength = 6f;

    [SerializeField] private float _ropeLength;
    // public GameObject ropeSegmentPrefab;

    // private List<GameObject> _ropeSegments = new List<GameObject>();
    // private float _distanceEachSegment = 2f;
    private DistanceJoint2D _directJoint;

    [Header("Rope Verlet")]

    [SerializeField] private int _numOfRopeSegments = 50;

    [SerializeField] private float _ropeVerletLengthMultiplier = 0.8f;
    private float _ropeSegmentLength;

    [Header("Rope Physics")]

    [SerializeField] private Vector2 _gravityForce = new Vector2(0f, -2f);
    [SerializeField] private float _dampingFactor = 0.98f;

    [Header("Constraints")]
    [SerializeField] private int _numOfConstraintRuns = 50;

    private Vector3 _ropeStartPoint;
    private Vector3 _ropeEndPoint;

    private List<RopeSegment> _ropeSegments = new List<RopeSegment>();

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _numOfRopeSegments;

        _ropeLength = _defaultRopeLength;
        _ropeSegmentLength = (float) _ropeLength / _numOfRopeSegments;
    }

    private void Start()
    {
        SetRopeLength(_defaultRopeLength);
    }


    private void Update()
    {
        _ropeStartPoint = player1.transform.position;
        _ropeEndPoint = player2.transform.position;
        UpdateAllLineRendererPointsPosition();
        SetRopeLength(_ropeLength);
    }

    private void FixedUpdate()
    {
        SimulateRopePhysics();
        
        for (int i = 0; i < _numOfConstraintRuns; i++)
        {
            ApplyConstraints();
        }
    }

    public void InitializeRope()
    {
        InitializePlayer();

        // Rope visual
        _ropeStartPoint = player1.transform.position;
        _ropeEndPoint = player2.transform.position;

        _ropeSegmentLength = (float) _defaultRopeLength / _numOfRopeSegments;

        for (int i = 0; i < _numOfRopeSegments; i++)
        {
            float t = (float)i / (_numOfRopeSegments - 1);
            Vector3 segmentPosition = Vector3.Lerp(_ropeStartPoint, _ropeEndPoint, t);
            _ropeSegments.Add(new RopeSegment(segmentPosition));
        }


        // Actual rope
        _directJoint = player1.AddComponent<DistanceJoint2D>();
        _directJoint.autoConfigureDistance = false;
        _directJoint.maxDistanceOnly = true;
        _directJoint.connectedBody = player2.GetComponent<Rigidbody2D>();
        _directJoint.enableCollision = true;
    }

    private void InitializePlayer()
    {
        if (player1 == null || player2 == null)
        {
            player1 = GameManager.Instance.currentPlayer1;
            player2 = GameManager.Instance.currentPlayer2;
        }

        // if (player1.GetComponent<DistanceJoint2D>() == null)
        // {
        //     DistanceJoint2D distanceJoint2D = player1.AddComponent<DistanceJoint2D>();
        //     distanceJoint2D.autoConfigureDistance = false;
        //     distanceJoint2D.maxDistanceOnly = true;
        // }

        // if (player2.GetComponent<DistanceJoint2D>() == null)
        // {
        //     DistanceJoint2D distanceJoint2D = player2.AddComponent<DistanceJoint2D>();
        //     distanceJoint2D.autoConfigureDistance = false;
        //     distanceJoint2D.maxDistanceOnly = true;
        // }
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
    //     Rigidbody2D midRigidBody = ropeSegmentMid.GetComponent<Rigidbody2D>();
    //     midRigidBody.mass = 0.0001f;
    //     ropeSegmentMid.transform.parent = transform; // Set parent to RopeManager
    //     _ropeSegments.Add(ropeSegmentMid);

    //     foreach (GameObject objTarget in new GameObject[] { player1, player2 })
    //     {
    //         for (int i = 1; i < ropeSegmentCountHalved; i++)
    //         {

    //             Vector3 position = Vector3.Lerp(midPoint, objTarget.transform.position, (float)i / ropeSegmentCountHalved);
    //             GameObject ropeSegment = Instantiate(ropeSegmentPrefab, position, Quaternion.identity);
    //             ropeSegment.name = "RopeSegment_" + objTarget.name + "_" + i;
    //             ropeSegment.tag = "RopeSegment";
    //             ropeSegment.transform.position = position;

    //             _ropeSegments.Add(ropeSegment);
    //             ropeSegment.transform.parent = transform; // Set parent to RopeManager

    //             DistanceJoint2D distanceJoint2D = ropeSegment.GetComponent<DistanceJoint2D>();
    //             distanceJoint2D.connectedBody = _ropeSegments[_ropeSegments.Count - 2].GetComponent<Rigidbody2D>();
    //         }

    //         DistanceJoint2D playerDistanceJoint2D = objTarget.GetComponent<DistanceJoint2D>();
    //         playerDistanceJoint2D.connectedBody = _ropeSegments[_ropeSegments.Count - 1].GetComponent<Rigidbody2D>();

    //         if (objTarget == player1)
    //         {
    //             _ropeSegments.Reverse(); // Reverse the order to connect from player1 to player2
    //         }

    //     }
    // }

    // private int GetTotalSegmentPoints()
    // {
    //     return ropeSegmentCountHalved * 2 + 1;
    // }

    // private void UpdateAllLineRendererPointsPosition()
    // {
    //     if (_lineRenderer == null || player1 == null || player2 == null || _ropeSegments.Count == 0)
    //     {
    //         return;
    //     }

    //     int totalPoints = GetTotalSegmentPoints();
    //     _lineRenderer.positionCount = totalPoints;

    //     // Use Vector3.Lerp for smoothing
    //     Vector3 prevPos = _lineRenderer.GetPosition(0);
    //     Vector3 targetPos = player1.transform.position;
    //     _lineRenderer.SetPosition(0, Vector3.Lerp(prevPos, targetPos, 0.5f));

    //     int segmentIndex = 1;
    //     for (int i = 0; i < _ropeSegments.Count; i++)
    //     {
    //         prevPos = _lineRenderer.GetPosition(segmentIndex);
    //         targetPos = _ropeSegments[i].transform.position;
    //         _lineRenderer.SetPosition(segmentIndex, Vector3.Lerp(prevPos, targetPos, 0.5f));
    //         segmentIndex++;
    //     }

    //     prevPos = _lineRenderer.GetPosition(totalPoints - 1);
    //     targetPos = player2.transform.position;
    //     _lineRenderer.SetPosition(totalPoints - 1, Vector3.Lerp(prevPos, targetPos, 0.5f));
    // }

    private void UpdateAllLineRendererPointsPosition()
    {
        // Vector3[] ropePositions = new Vector3[_numOfRopeSegments];

        // for (int i = 0; i < _ropeSegments.Count; i++)
        // {
        //     ropePositions[i] = _ropeSegments[i].CurrentPosition;
        // }

        // _lineRenderer.SetPositions(ropePositions);

        //Use Vector3.Lerp for smoothing
        Vector3 prevPos = _lineRenderer.GetPosition(0);
        Vector3 targetPos = player1.transform.position;
        _lineRenderer.SetPosition(0, Vector3.Lerp(prevPos, targetPos, 0.5f));

        for (int i = 1; i < _ropeSegments.Count - 1; i++)
        {
            prevPos = _lineRenderer.GetPosition(i);
            targetPos = _ropeSegments[i].CurrentPosition;
            _lineRenderer.SetPosition(i, Vector3.Lerp(prevPos, targetPos, 0.5f));
        }

        prevPos = _lineRenderer.GetPosition(_ropeSegments.Count - 1);
        targetPos = player2.transform.position;
        _lineRenderer.SetPosition(_ropeSegments.Count - 1, Vector3.Lerp(prevPos, targetPos, 0.5f));
    }

    private void SimulateRopePhysics()
    {
        // Apply gravity and damping to each segment
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            RopeSegment segment = _ropeSegments[i];
            Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * _dampingFactor;

            segment.OldPosition = segment.CurrentPosition;
            segment.CurrentPosition += velocity;
            segment.CurrentPosition += _gravityForce * Time.fixedDeltaTime;
            _ropeSegments[i] = segment;
        }
    }

    private void ApplyConstraints()
    {
        RopeSegment firstSegment = _ropeSegments[0];
        RopeSegment lastSegment = _ropeSegments[_ropeSegments.Count - 1];
        firstSegment.CurrentPosition = _ropeStartPoint;
        lastSegment.CurrentPosition = _ropeEndPoint;

        _ropeSegments[0] = firstSegment;
        _ropeSegments[_ropeSegments.Count - 1] = lastSegment;

        for (int i = 0; i < _ropeSegments.Count - 1; i++)
        {
            RopeSegment currentSeg = _ropeSegments[i];
            RopeSegment nextSeg = _ropeSegments[i + 1];

            float dist = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).magnitude;
            float difference = (_ropeSegmentLength * _ropeVerletLengthMultiplier) - dist;

            Vector2 changeDir = (nextSeg.CurrentPosition - currentSeg.CurrentPosition).normalized;
            Vector2 changeVector = changeDir * difference;

            if (i != 0)
            {
                currentSeg.CurrentPosition -= changeVector * 0.5f;
                nextSeg.CurrentPosition += changeVector * 0.5f;
            }
            else
            {
                nextSeg.CurrentPosition += changeVector;
            }

            _ropeSegments[i] = currentSeg;
            _ropeSegments[i + 1] = nextSeg;
        }
    }

    // public void SetRopeLength(float length)
    // {
    //     _distanceEachSegment = length / (ropeSegmentCountHalved * 2);

    //     // Update the distance for each segment
    //     foreach (GameObject segment in _ropeSegments)
    //     {
    //         if (segment != null && segment.GetComponent<DistanceJoint2D>() != null)
    //         {
    //             segment.GetComponent<DistanceJoint2D>().distance = _distanceEachSegment;
    //         }
    //     }

    //     if (player1.GetComponent<DistanceJoint2D>() != null)
    //     {
    //         DistanceJoint2D distanceJoint2D = player1.GetComponent<DistanceJoint2D>();
    //         distanceJoint2D.distance = _distanceEachSegment;
    //     }

    //     if (player2.GetComponent<DistanceJoint2D>() != null)
    //     {
    //         DistanceJoint2D distanceJoint2D = player2.GetComponent<DistanceJoint2D>();
    //         distanceJoint2D.distance = _distanceEachSegment;
    //     }

    //     _directJoint.distance = length;

    // }

    public void SetRopeLength(float length)
    {
        _ropeLength = length;
        _ropeSegmentLength = (float) length / _numOfRopeSegments;

        _directJoint.distance = length;
    }

    public void DestroyRope()
    {
        // foreach (GameObject segment in _ropeSegments)
        // {
        //     if (segment != null)
        //     {
        //         Destroy(segment);
        //     }
        // }

        _ropeSegments.Clear();
        // _lineRenderer.positionCount = 0;
    }

    public struct RopeSegment
    {
        public Vector2 CurrentPosition;
        public Vector2 OldPosition;

        public RopeSegment(Vector2 pos)
        {
            CurrentPosition = pos;
            OldPosition = pos;
        }


    }
}