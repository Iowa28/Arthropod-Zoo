using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class SpiderProceduralAnimation : MonoBehaviour {
    [SerializeField]
    private Transform[] legTargets;
    [SerializeField] 
    private double[] stepSizes;
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
    
    private Vector3[] defaultLegPositions;
    private Vector3[] lastLegPositions;
    private Vector3 lastBodyUp;
    private bool[] legMoving;
    private int legsNumber;

    private int groupToMove = 0;
    private bool group1Moving;
    private bool group2Moving;
    
    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastBodyPos;
    private readonly float velocityMultiplier = 30f;

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

        // Debug.Log("Step sizes = " +String.Join("",
        //     new List<double>(stepSizes)
        //         .ConvertAll(i => i.ToString(CultureInfo.InvariantCulture))
        //         .ToArray()));

        FixBodyOrientation();
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
        return !legMoving[0] && !legMoving[1] && !legMoving[2] && !legMoving[3];
    }

    private bool IsSecondGroupNotMoving()
    {
        return !legMoving[4] && !legMoving[5] && !legMoving[6] && !legMoving[7];
    }

    private void MoveGroupedLegs()
    {
        if (IsFirstGroupNotMoving() && IsSecondGroupNotMoving())
        {
            if (groupToMove == 0)
            {
                MoveLegs(0, 4);

                groupToMove = 1;
            }
            else if (groupToMove == 1)
            {
                MoveLegs(4, 8);

                groupToMove = 0;
            }
        }

        for (int i = 0; i < 8; i++)
        {
            if (!legMoving[i])
            {
                legTargets[i].position = lastLegPositions[i];
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

    private void MoveLegs()
    {
        // ищется нога, которая расположена от своего дефотного положения дальше всего
        Vector3[] desiredPositions = new Vector3[legsNumber];
        int indexToMove = -1;
        float maxDistance = stepSize;
        for (int i = 0; i < legsNumber; i++) {
            desiredPositions[i] = transform.TransformPoint(defaultLegPositions[i]);
            float distance = Vector3.ProjectOnPlane(
                desiredPositions[i] + velocity * velocityMultiplier - lastLegPositions[i],
                transform.up
                ).magnitude;
            if (distance > maxDistance) {
                maxDistance = distance;
                indexToMove = i;
            }
        }

        // остальные ноги сохраняют свое положение
        for (int i = 0; i < legsNumber; ++i) {
            if (i != indexToMove) {
                legTargets[i].position = lastLegPositions[i];
            }
        }

        // если выбранная нога не движется, то двигаем
        if (indexToMove != -1 && !legMoving[indexToMove]) {
            MoveLeg(indexToMove, desiredPositions[indexToMove]);
        }

        lastBodyPos = transform.position;
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

    private void FixBodyOrientation() {
        if (legsNumber > 3 && bodyOrientation) {
            Vector3 v1 = legTargets[0].position - legTargets[2].position;
            Vector3 v2 = legTargets[4].position - legTargets[7].position;
            Vector3 normal = Vector3.Cross(v1, v2).normalized;
            Vector3 up = Vector3.Lerp(lastBodyUp, normal, 1f / (smoothness + 1));
            transform.up = up;
            transform.rotation = Quaternion.LookRotation(transform.parent.forward, up);
            lastBodyUp = transform.up;
        }
    }

    private void OnDrawGizmosSelected() {
        for (int i = 0; i < legsNumber; ++i) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(legTargets[i].position, 0.05f);
            Gizmos.color = Color.green;
            // Gizmos.DrawWireSphere(transform.TransformPoint(defaultLegPositions[i]), stepSize);
            Gizmos.DrawWireSphere(transform.TransformPoint(defaultLegPositions[i]), (float) stepSizes[i]);
        }
    }
}
