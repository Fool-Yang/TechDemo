using System.Collections.Generic;
using UnityEngine;
using UnityMovementAI;
using UnityBehaviourAI;
using Management;
using Generator;
using NPBehave;

namespace Agent
{
    // Behaviour Tree agent with Waypoints Navigation
    public class GuardUnit : MonoBehaviour
    {
        [System.NonSerialized]
        public MovementAIRigidbody target;

        SteeringBasics steeringBasics;
        WallAvoidance wallAvoid;
        UnitEvent uevent;
        GameManager gm;
        DungeonGenerator dg;
        Stack<Vector3> chasePath = null;
        Stack<Vector3> investigatePath = null;
        Stack<Vector3> patrolPath = null;
        float roomSize;
        float chaseDist;

        Root tree;

        void Start()
        {
            gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            target = GameObject.Find("PlayerUnit(Clone)").GetComponent<MovementAIRigidbody>();
            steeringBasics = GetComponent<SteeringBasics>();
            wallAvoid = GetComponent<WallAvoidance>();
            wallAvoid.wallDetection = WallAvoidance.WallDetection.Raycast;
            uevent = GetComponent<UnitEvent>();
            dg = GameObject.Find("DungeonGenerator").GetComponent<DungeonGenerator>();
            roomSize = (float)dg.roomSize;
            chaseDist = 1.5f*roomSize;

            // behaviour tree init
            tree = CreateBehaviourTree();
            tree.Start();
        }

        /**************************************
         * 
         * Behaviour Tree
         * 
         */
        Root CreateBehaviourTree() {
            return new Root(
                new Selector(
                    Idle(),
                    Chase(),
                    Investigate(),
                    Patrol()
                )
            );
        }

        Node Idle() {
            return new Condition(() => target == null, new Action(() => IdleAction()));
        }

        Node Chase() {
            return new Selector(
                new Condition(() => uevent.Contact(target), new Action(() => KillAction())),
                new Condition(() => IsInSight(target.Position), new Action(() => ChaseAction()))
            );
        }

        Node Investigate() {
            return new Condition(
                () => gm.alertPosition != gm.nullAlert && gm.dummyInstance != null,
                new Sequence(
                    new Action(() => SetInvestigatePath()),
                    new Action(() => InvestigateAction())
                )
            );
        }

        Node Patrol() {
            return new Selector(
                new Condition(() => patrolPath == null, new Action(() => SetPatrolPath())),
                new Action(() => PatrolAction())
            );
        }

        // perception
        bool IsInSight(Vector3 positon) {
            return FindPathTo(positon).Count <= 7 && Vector3.Distance(positon, transform.position) <= chaseDist;
        }

        /**************************************
         * 
         * Actions
         * 
         */
        void IdleAction() {}

        void KillAction() {
            Destroy(target.gameObject, 0.3f);
        }

        void ChaseAction() {
            // destroy outdated path
            patrolPath = null;

            chasePath = FindPathTo(target.Position);
            Vector3 nextPoint;
            if (chasePath.Count <= 3) {
                // vecry close, steer towards the player directly
                nextPoint = target.Position;
            } else {
                // go to the next waypoint
                nextPoint = chasePath.Peek();
                if (uevent.Arrived(nextPoint)) {
                    nextPoint = chasePath.Pop();
                    nextPoint = chasePath.Peek();
                }
            }

            // steer
            Vector3 facing = GetComponent<MovementAIRigidbody>().Velocity;
            Vector3 accel = steeringBasics.Seek(nextPoint);
            Vector3 avoid = wallAvoid.GetSteering(facing);

            if (avoid.magnitude >= 0.005f)
            {
                accel = avoid;
            }

            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }

        void SetInvestigatePath() {
            investigatePath = FindPathTo(gm.dummyInstance.position);
        }

        void InvestigateAction() {
            // destroy outdated path
            patrolPath = null;

            // if catch the dummy, destroy it
            if (uevent.Arrived(gm.dummyInstance.position)) {
                // turn off the alarm
                gm.alertPosition = gm.nullAlert;
                Destroy(gm.dummyInstance.gameObject);
                return;
            }

            // otherwise go to the next waypoint
            Vector3 nextPoint = investigatePath.Peek();
            if (uevent.Arrived(nextPoint)) {
                nextPoint = investigatePath.Pop();
                nextPoint = investigatePath.Peek();
            }

            // steer towards the waypoint
            Vector3 facing = GetComponent<MovementAIRigidbody>().Velocity;
            Vector3 accel = steeringBasics.Seek(nextPoint);
            Vector3 avoid = wallAvoid.GetSteering(facing);

            if (avoid.magnitude >= 0.005f)
            {
                accel = avoid;
            }

            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }

        void SetPatrolPath() {
            // set random destination
            Vector3 dest;
            Vector3 myRoom = NearestRoom(transform.position);
            do {
                dest = dg.Waypoints[UnityEngine.Random.Range(0, dg.Waypoints.Count)];
            } while (dest == myRoom);
            patrolPath = FindPathTo(dest);
        }

        void PatrolAction() {
            Vector3 nextPoint = patrolPath.Peek();
            // pop the path queue if we arraive at the next waypoint
            if (uevent.Arrived(nextPoint)) {
                nextPoint = patrolPath.Pop();
                if (patrolPath.Count > 0) {
                    nextPoint = patrolPath.Peek();
                // delete the path and finish this patrol if no waypoints left
                } else {
                    patrolPath = null;
                    return;
                }
            }

            // steer towards the waypoint
            Vector3 facing = GetComponent<MovementAIRigidbody>().Velocity;
            Vector3 accel = steeringBasics.Seek(nextPoint);
            Vector3 avoid = wallAvoid.GetSteering(facing);

            if (avoid.magnitude >= 0.005f)
            {
                accel = avoid;
            }

            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();

        }

        /**************************************
         * 
         * Graph Search (navigation waypoints)
         * 
         */
        Stack<Vector3> FindPathTo(Vector3 end) {
            // BFS
            Vector3 startWaypoint = NearestWaypoint(transform.position);
            Vector3 endWaypoint = NearestWaypoint(end);
            Queue<Vector3> queue = new Queue<Vector3>();
            Dictionary<Vector3, Vector3> prev = new Dictionary<Vector3, Vector3>();
            queue.Enqueue(startWaypoint);
            prev[startWaypoint] = gm.nullAlert;
            Vector3 curr;
            while (queue.Count > 0) {
                curr = queue.Dequeue();
                foreach (Vector3 child in dg.Neighbors[curr]) {
                    if (!prev.ContainsKey(child)) {
                        prev[child] = curr;
                        queue.Enqueue(child);
                    }
                }
            }
            // trace the path
            Stack<Vector3> path = new Stack<Vector3>();
            // if the end is not at the center of a room, add as the last waypoint
            if (end != endWaypoint) {
                path.Push(end);
            }
            curr = endWaypoint;
            while (curr != startWaypoint) {
                path.Push(curr);
                curr = prev[curr];
            }
            return path;
        }

        Vector3 NearestRoom(Vector3 position) {
            Vector3 point = new Vector3(-1f, -1f, -1f);
            float dist = Vector3.Distance(point, position);
            foreach (Vector3 roomCenter in dg.Waypoints) {
                float newDist = Vector3.Distance(roomCenter, position);
                if (newDist < dist) {
                    point = roomCenter;
                    dist = newDist;
                }
            }
            return point;
        }

        Vector3 NearestWaypoint(Vector3 position) {
            Vector3 point = new Vector3(-1f, -1f, -1f);
            float dist = Vector3.Distance(point, position);
            foreach (Vector3 waypoint in dg.HRWaypoints) {
                float newDist = Vector3.Distance(waypoint, position);
                if (newDist < dist) {
                    point = waypoint;
                    dist = newDist;
                }
            }
            return point;
        }
    }
}