using UnityEngine;
using UnityMovementAI;
using Generator;
using Management;

namespace Agent
{
    // Finite State Machine agent
    public class OverseerUnit : MonoBehaviour
    {
        SpriteRenderer renderer;
        MovementAIRigidbody rb;
        MovementAIRigidbody target;
        SteeringBasics steeringBasics;
        float roomSize;

        public enum State {active, idle, dead};
        [System.NonSerialized]
        public State state;
        GameManager gm;

        void Start()
        {
            renderer = GetComponent<SpriteRenderer>();
            gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            rb = GetComponent<MovementAIRigidbody>();
            target = GameObject.Find("PlayerUnit(Clone)").GetComponent<MovementAIRigidbody>();
            steeringBasics = GetComponent<SteeringBasics>();

            DungeonGenerator dg = GameObject.Find("DungeonGenerator").GetComponent<DungeonGenerator>();
            roomSize = (float)dg.roomSize;

            // init state
            state = State.idle;
        }

        void FixedUpdate()
        {
            // FSM
            if (state == State.idle) {
                Idle();
            } else if (state == State.active) {
                Active();
            } else {
                Dead();
            }
        }

        void Idle() {
            // state transitions
            if (target != null && Vector3.Distance(target.Position, transform.position) <= roomSize) {
                state = State.active;
                return;
            }
            rb.Velocity *= 0.99f;
            renderer.color = new Color(1f, 1f, 0f, 1f);
        }

        void Active() {
            // state transitions
            if (target == null || Vector3.Distance(target.Position, transform.position) > roomSize) {
                state = State.idle;
                return;
            }
            // alarm
            gm.alertPosition = target.Position;
            gm.alertQuaternion = target.GetComponent<Transform>().rotation;
            Vector3 accel = steeringBasics.Seek(target.Position);
            
            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
            rb.Velocity = new Vector3(0, 0, 0);
            renderer.color = new Color(1f, 160f/255f, 0f, 1f);
        }

        void Dead() {}
    }
}