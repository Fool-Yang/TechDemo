using UnityEngine;

namespace UnityMovementAI
{
    public class WanderAvoidUnit : MonoBehaviour
    {
        SteeringBasics steeringBasics;

        void Start()
        {
            steeringBasics = GetComponent<SteeringBasics>();
        }

    }
}