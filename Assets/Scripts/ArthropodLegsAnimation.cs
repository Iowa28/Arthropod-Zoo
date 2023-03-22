using System.Collections;
using UnityEngine;

public class ArthropodLegsAnimation : MonoBehaviour {
    [SerializeField]
    private Transform[] legTargets;
    [SerializeField]
    private float stepSize = 0.15f;
    [SerializeField]
    private int smoothness = 8;
    [SerializeField]
    private float stepHeight = 0.15f;
    [SerializeField]
    private float sphereCastRadius = 0.125f;
    [SerializeField]
    private bool bodyOrientation = true;
    [SerializeField]
    private float raycastRange = 1.5f;
    [SerializeField]
    private Vector3 defaultLegOffset;
    
    private Vector3[] defaultLegPositions;
    private Vector3[] lastLegPositions;
    private Vector3 lastBodyUp;
    private bool[] legMoving;
    private int legsNumber;

    private int groupToMove;
    private bool group1Moving;
    private bool group2Moving;
    
    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastBodyPos;
    private const float velocityMultiplier = 30f;

    private void Start() {
        lastBodyUp = transform.up;

        legsNumber = legTargets.Length;
        defaultLegPositions = new Vector3[legsNumber];
        lastLegPositions = new Vector3[legsNumber];
        legMoving = new bool[legsNumber];
        for (int i = 0; i < legsNumber; i++) {
            defaultLegPositions[i] = legTargets[i].localPosition;
            lastLegPositions[i] = legTargets[i].position;
            legMoving[i] = false;
        }
        lastBodyPos = transform.position;
    }

    private void FixedUpdate() {
        CalculateVelocity();

        MoveGroupedLegs();

        if (bodyOrientation && legsNumber == 8)
        {
            FixBodyOrientation();   
        }
    }

    private void CalculateVelocity() {
        // вычисляем вектор движения тела паука
        velocity = transform.position - lastBodyPos;
        velocity = (velocity + smoothness * lastVelocity) / (smoothness + 1f);

        const float delta = 0.000025f;
        if (velocity.magnitude < delta) {
            velocity = lastVelocity;
        }
        else {
            lastVelocity = velocity;
        }
    }

    private bool IsFirstGroupNotMoving()
    {
        bool groupNotMoving = !legMoving[0];
        
        for (int i = 1; i < legsNumber / 2; i++)
        {
            groupNotMoving = groupNotMoving && !legMoving[i];
        }

        return groupNotMoving;
    }

    private bool IsSecondGroupNotMoving()
    {
        bool groupNotMoving = !legMoving[legsNumber / 2];
        
        for (int i = legsNumber / 2 + 1; i < legsNumber; i++)
        {
            groupNotMoving = groupNotMoving && !legMoving[i];
        }

        return groupNotMoving;
    }

    private void MoveGroupedLegs()
    {
        if (IsFirstGroupNotMoving() && IsSecondGroupNotMoving())
        {
            if (groupToMove == 0)
            {
                MoveLegs(0, legsNumber / 2);

                groupToMove = 1;
            }
            else if (groupToMove == 1)
            {
                MoveLegs(legsNumber / 2, legsNumber);
                
                groupToMove = 0;
            }
        }

        for (int i = 0; i < legsNumber; i++)
        {
            if (!legMoving[i])
            {
                legTargets[i].position = lastLegPositions[i] + defaultLegOffset;
            }
        }
        
        lastBodyPos = transform.position;
    }

    private void MoveLegs(int from, int to)
    {
        for (int i = from; i < to; i++)
        {
            Vector3 desiredPosition = transform.TransformPoint(defaultLegPositions[i]);
            float distance = Vector3.ProjectOnPlane(
                desiredPosition + velocity * velocityMultiplier - lastLegPositions[i],
                transform.up
            ).magnitude;

            if (!legMoving[i] && distance > stepSize)
            {
                MoveLeg(i, desiredPosition);   
            }
            else
            {
                legTargets[i].position = lastLegPositions[i];
            }
        }
    }

    private void MoveLeg(int indexToMove, Vector3 desiredPosition)
    {
        // вычисление точки, в которую нога должна переместиться 
        Vector3 targetPoint = desiredPosition + Mathf.Clamp(velocity.magnitude * velocityMultiplier, 0.0f, 1.5f) * (desiredPosition - legTargets[indexToMove].position);
        targetPoint += velocity * velocityMultiplier;
            
        Vector3[] positionAndNormalForward = MatchToSurfaceFromAbove(
            targetPoint, 
            raycastRange, 
            (transform.parent.up - velocity * 100).normalized);
        Vector3[] positionAndNormalBackward = MatchToSurfaceFromAbove(
            targetPoint, 
            raycastRange * (1f + velocity.magnitude), 
            (transform.parent.up + velocity * 75).normalized);
            
        legMoving[indexToMove] = true;
            
        if (positionAndNormalForward[1] != Vector3.zero) {
            StartCoroutine(PerformStep(indexToMove, positionAndNormalForward[0]));
        }
        else {
            StartCoroutine(PerformStep(indexToMove, positionAndNormalBackward[0]));
        }
    }

    private Vector3[] MatchToSurfaceFromAbove(Vector3 point, float halfRange, Vector3 up) {
        Vector3[] res = new Vector3[2];
        res[1] = Vector3.zero;
        RaycastHit hit;
        Ray ray = new Ray(point + halfRange * up / 2f, -up);

        if (Physics.SphereCast(ray, sphereCastRadius, out hit, 2f * halfRange)) {
            res[0] = hit.point;
            res[1] = hit.normal;
        }
        else {
            res[0] = point;
        }
        return res;
    }

    private IEnumerator PerformStep(int index, Vector3 targetPoint) {
        Vector3 startPos = lastLegPositions[index];
        for(int i = 1; i <= smoothness; i++) {
            legTargets[index].position = Vector3.Lerp(startPos, targetPoint, i / (smoothness + 1f));
            legTargets[index].position += transform.up * Mathf.Sin(i / (smoothness + 1f) * Mathf.PI) * stepHeight;
            yield return new WaitForFixedUpdate();
        }
        legTargets[index].position = targetPoint;
        lastLegPositions[index] = legTargets[index].position;
        legMoving[index] = false;
    }

    private void FixBodyOrientation()
    {
        Vector3 v1 = default, v2 = default;
        
        if (legsNumber / 2 == 4) 
        {
            v1 = legTargets[0].position - legTargets[3].position;
            v2 = legTargets[4].position - legTargets[7].position;
        }
        else if (legsNumber / 2 == 3)
        {
            v1 = legTargets[0].position - legTargets[2].position;
            v2 = legTargets[3].position - legTargets[5].position;
        }

        if (v1 != default && v2 != default)
        {
            Vector3 normal = Vector3.Cross(v1, v2).normalized;
            Vector3 up = Vector3.Lerp(lastBodyUp, normal, 1f / (smoothness + 1));
            transform.up = up;
            transform.rotation = Quaternion.LookRotation(transform.parent.forward, up);
            lastBodyUp = transform.up;
        }
    }

    private void OnDrawGizmosSelected()
    {
        const float legRadius = .05f; 
        for (int i = 0; i < legsNumber; ++i) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(legTargets[i].position, legRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(defaultLegPositions[i]), stepSize);
        }
    }
}
