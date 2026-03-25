//using UnityEngine;

//public sealed class PassengerWalker : MonoBehaviour
//{
//    [SerializeField] private float moveSpeed = 1.6f;
//    [SerializeField] private float rotateSpeed = 10f;
//    [SerializeField] private float arriveDistance = 0.05f;

//    private Transform target;

//    public void SetTarget(Transform newTarget)
//    {
//        target = newTarget;
//    }

//    public bool HasTarget => target != null;

//    private void Update()
//    {
//        if (target == null) return;

//        Vector3 to = target.position - transform.position;
//        to.y = 0f;

//        float dist = to.magnitude;
//        if (dist <= arriveDistance)
//            return;

//        Vector3 dir = to.normalized;

//        transform.position += dir * (moveSpeed * Time.deltaTime);

//        if (dir.sqrMagnitude > 0.0001f)
//        {
//            Quaternion desired = Quaternion.LookRotation(dir, Vector3.up);
//            transform.rotation = Quaternion.Slerp(transform.rotation, desired, rotateSpeed * Time.deltaTime);
//        }
//    }
//}
