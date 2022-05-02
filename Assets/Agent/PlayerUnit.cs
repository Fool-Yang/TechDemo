using UnityEngine;
using UnityMovementAI;

namespace Agent
{
    [RequireComponent(typeof(SteeringBasics))]
    public class PlayerUnit : MonoBehaviour
    {
        public Vector3 direction;

        MovementAIRigidbody rb;
        SteeringBasics steeringBasics;

        void Start()
        {
            rb = GetComponent<MovementAIRigidbody>();
            steeringBasics = GetComponent<SteeringBasics>();
            direction = new Vector3(0, 0, 0);
        }

        void FixedUpdate()
        {
            // init direction to <0, 0>
            direction.x = 0;
            direction.y = 0;

            // check for input and add direction vectors accordingly, this allows for a combination of inputs
            if (Input.GetKey("w")) {
                direction.y += 1;
            }
            if (Input.GetKey("s")) {
                direction.y -= 1;
            }
            if (Input.GetKey("a")) {
                direction.x -= 1;
            }
            if (Input.GetKey("d")) {
                direction.x += 1;
            }

            rb.Velocity = direction.normalized * steeringBasics.maxVelocity;

            steeringBasics.LookWhereYoureGoing();

            Debug.DrawLine(rb.ColliderPosition, rb.ColliderPosition + (direction.normalized), Color.cyan, 0f, false);
        }
    }
}