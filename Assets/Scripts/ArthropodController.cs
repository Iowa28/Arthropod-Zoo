using UnityEngine;

public class ArthropodController : MonoBehaviour {
    [SerializeField]
    private float speed = 3f;
    [SerializeField]
    private float smoothness = 5f;
    [SerializeField]
    private float raysEccentricity = 0.2f;
    [SerializeField]
    private float innerRaysOffset = 25f;
    [SerializeField]
    private float outerRaysOffset = 2f;
    [SerializeField]
    private int rayAmount = 8;

    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastPosition;
    private Vector3 forward;
    private Quaternion lastRot;
    private Vector3[] pn;

    private void Start() {
        velocity = new Vector3();
        forward = transform.forward;
        lastRot = transform.rotation;
        lastPosition = transform.position;
    }

    private void CalculateVelocity() {
        // velocity отвечает за поворот камеры
        velocity = (smoothness * velocity + (transform.position - lastPosition)) / (1f + smoothness);
        // это нужно, чтобы камера вращалась равномерно
        const float delta = .00025f;
        if (velocity.magnitude < delta) {
            velocity = lastVelocity;
        }

        lastPosition = transform.position;
        lastVelocity = velocity;
    }

    private void FixedUpdate() {
        CalculateVelocity();
        
        float fixedSpeed = speed * Time.fixedDeltaTime;
        if (Input.GetKey(KeyCode.LeftShift)) {
            fixedSpeed *= 2f;
        }

        float valueY = Input.GetAxis("Vertical");
        if (valueY != 0) {
            transform.position += transform.forward * valueY * fixedSpeed;
        }

        float valueX = Input.GetAxis("Horizontal");
        if (valueX != 0) {
            transform.position += transform.right * valueX * fixedSpeed * .5f;
        }

        if (valueX != 0 || valueY != 0) {
            Vector3 closestPointPosition = GetClosestPointPosition(.5f);
            Vector3 closestPointNormal = GetClosestPointNormal(.5f);

            transform.position = Vector3.Lerp(lastPosition, closestPointPosition, 1f / (1f + smoothness));

            forward = velocity.normalized;
            Quaternion lookRotation = Quaternion.LookRotation(forward, closestPointNormal);
            transform.rotation = Quaternion.Lerp(lastRot, lookRotation, 1f / (1f + smoothness));
        }

        lastRot = transform.rotation;
    }

    private Vector3[] CalculateRayDirections() {
        Vector3 right = Vector3.Cross(transform.up, transform.forward);

        Vector3[] rayDirections = new Vector3[rayAmount];
        float angularStep = 2f * Mathf.PI / rayAmount;
        float currentAngle = angularStep / 2f;
        for(int i = 0; i < rayAmount; ++i) {
            rayDirections[i] = -transform.up + (right * Mathf.Cos(currentAngle) + transform.forward * Mathf.Sin(currentAngle)) * raysEccentricity;
            currentAngle += angularStep;
        }

        return rayDirections;
    }

    private Vector3 GetClosestPointPosition(float halfRange) {
        Vector3 result = transform.position;
        Vector3[] dirs = CalculateRayDirections();
        float positionAmount = 1f;
        
        foreach (Vector3 dir in dirs) {
            RaycastHit hit;
            
            Ray ray = GetRayByDirection(dir, halfRange, innerRaysOffset);
            if (Physics.SphereCast(ray, .01f, out hit, 2f * halfRange)) {
                result += hit.point;
                positionAmount += 1;
            }
            
            ray = GetRayByDirection(dir, halfRange, outerRaysOffset);
            if (Physics.SphereCast(ray, .01f, out hit, 2f * halfRange)) {
                result += hit.point;
                positionAmount += 1;
            }
        }
        result /= positionAmount;
        return result;
    }

    private Vector3 GetClosestPointNormal(float halfRange) {
        Vector3 result = transform.up;
        Vector3[] dirs = CalculateRayDirections();
        float normalAmount = 1f;
        
        foreach (Vector3 dir in dirs) {
            RaycastHit hit;
            Ray ray = GetRayByDirection(dir, halfRange, innerRaysOffset);
            if (Physics.SphereCast(ray, .01f, out hit, 2f * halfRange)) {
                result += hit.normal;
                normalAmount += 1;
            }
            
            ray = GetRayByDirection(dir, halfRange, outerRaysOffset);
            if (Physics.SphereCast(ray, .01f, out hit, 2f * halfRange)) {
                result += hit.normal;
                normalAmount += 1;
            }
        }
        result /= normalAmount;
        return result;
    }

    private Ray GetRayByDirection(Vector3 direction, float halfRange, float offset) {
        Vector3 largener = Vector3.ProjectOnPlane(direction, transform.up);
        Ray ray = new Ray(
            transform.position - (direction + largener) * halfRange + largener.normalized * offset / 100f, 
            direction);
        return ray;
    }
}
