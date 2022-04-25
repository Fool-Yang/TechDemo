using UnityEngine;
using UnityMovementAI;

namespace Management
{
    public class CameraManager : MonoBehaviour
    {
        MovementAIRigidbody rb;

        public void FindPlayer()
        {
            rb = GameObject.Find("PlayerUnit(Clone)").GetComponent<MovementAIRigidbody>();
        }

        void FixedUpdate()
        {
            // follow the player if it's not dead
            if (rb != null) {
                Vector3 pos = transform.position;

                pos.x = rb.Position.x;
                pos.y = rb.Position.y;

                transform.position = pos;
            }
        }
    }
}