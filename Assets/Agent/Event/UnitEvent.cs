using UnityEngine;
using UnityMovementAI;

namespace UnityBehaviourAI
{
    public class UnitEvent : MonoBehaviour
    {
        public float collideDist = 0.65f;
        public float arriveDist = 1.6f;

        /// <summary>
        /// Returns whether it collides with the target
        /// </summary>
        public bool Contact(MovementAIRigidbody target) {
            /* Get the distance to the target */
            Vector3 distVector3 = target.Position - transform.position;
            Vector2 distVector2 = new Vector2(distVector3.x, distVector3.y);
            float dist = distVector2.magnitude;
            return dist <= collideDist;
        }

        public bool Arrived(Vector3 position) {
            Vector3 distVector3 = position - transform.position;
            Vector2 distVector2 = new Vector2(distVector3.x, distVector3.y);
            float dist = distVector2.magnitude;
            return dist <= arriveDist;
        }
    }
}