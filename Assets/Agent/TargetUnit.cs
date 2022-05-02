using UnityEngine;
using UnityMovementAI;
using UnityBehaviourAI;
using Management;
using Generator;

namespace Agent
{
    // Finite State Machine agent
    public class TargetUnit : MonoBehaviour
    {
        public enum State {active, idle, dead};
        [System.NonSerialized]
        public State state;

        MovementAIRigidbody target;

        SteeringBasics steeringBasics;
        Hide hide;
        ObstacleSpawner obstacleSpawner;
        UnitSpawner unitSpawner;
        WallAvoidance wallAvoid;
        UnitEvent uevent;
        float roomSize;
        GameManager gm;

        void Start()
        {
            gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            target = GameObject.Find("PlayerUnit(Clone)").GetComponent<MovementAIRigidbody>();
            steeringBasics = GetComponent<SteeringBasics>();
            hide = GetComponent<Hide>();
            obstacleSpawner = GameObject.Find("ObstacleSpawner").GetComponent<ObstacleSpawner>();
            unitSpawner = GameObject.Find("UnitSpawner").GetComponent<UnitSpawner>();
            wallAvoid = GetComponent<WallAvoidance>();
            uevent = GetComponent<UnitEvent>();

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
        }

        void Active() {
            // state transitions
            if (target == null || Vector3.Distance(target.Position, transform.position) > roomSize) {
                state = State.idle;
                return;
            }
            // if caught by the palyer, die
            if (uevent.Contact(target)) {
                Destroy(gameObject, 0.3f);
                state = State.dead;
                gm.numTargets--;
            }
            // otherwise run away
            Vector3 hidePosition;
            Vector3 hideAccel = hide.GetSteering(target, obstacleSpawner.Objs, out hidePosition);

            Vector3 accel = wallAvoid.GetSteering(hidePosition - transform.position);

            if (accel.magnitude < 0.005f)
            {
                accel = hideAccel;
            }

            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }

        void Dead() {}
    }
}