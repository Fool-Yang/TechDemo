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
            Vector3 myRoom = NearestWaypoint(transform.position);
            Vector3 targetRoom = NearestWaypoint(positon);
            List<Vector3> neighbors = Neighbors(myRoom);
            neighbors.Add(myRoom);
            foreach (Vector3 neighbor in neighbors) {
                if (neighbor == targetRoom) {
                    return Vector3.Distance(positon, transform.position) <= chaseDist;
                }
            }
            return false;
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

            // steer towards the player
            Vector3 facing = GetComponent<MovementAIRigidbody>().Velocity;
            Vector3 accel = steeringBasics.Seek(target.Position);
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
            Vector3 nextPoint = investigatePath.Peek();
            if (uevent.Arrived(nextPoint)) {
                nextPoint = investigatePath.Pop();
                if (investigatePath.Count > 0) {
                    nextPoint = investigatePath.Peek();
                } else {
                    investigatePath = null;
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

        void SetPatrolPath() {
            // set random destination
            Vector3 dest;
            Vector3 myRoom = NearestWaypoint(transform.position);
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
            Vector3 startRoom = NearestWaypoint(transform.position);
            Vector3 endRoom = NearestWaypoint(end);
            Queue<Vector3> queue = new Queue<Vector3>();
            Dictionary<Vector3, Vector3> prev = new Dictionary<Vector3, Vector3>();
            queue.Enqueue(startRoom);
            prev[startRoom] = gm.nullAlert;
            Vector3 curr;
            while (queue.Count > 0) {
                curr = queue.Dequeue();
                foreach (Vector3 child in Neighbors(curr)) {
                    if (!prev.ContainsKey(child)) {
                        prev[child] = curr;
                        queue.Enqueue(child);
                    }
                }
            }
            // trace the path
            Stack<Vector3> path = new Stack<Vector3>();
            // if the end is not at the center of a room, add as the last waypoint
            if (end != endRoom) {
                path.Push(end);
            }
            curr = endRoom;
            while (prev[curr] != gm.nullAlert) {
                path.Push(curr);
                curr = prev[curr];
            }
            return path;
        }

        Vector3 NearestWaypoint(Vector3 position) {
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

        List<Vector3> Neighbors(Vector3 roomCenter) {
            // find all possible neighbors and check connectivity
            List<Vector3> neighbors = new List<Vector3>();
            Vector3 leftCorridor = new Vector3(roomCenter.x - roomSize, roomCenter.y, 0);
            Vector3 rightCorridor = new Vector3(roomCenter.x + roomSize, roomCenter.y, 0);
            Vector3 downCorridor = new Vector3(roomCenter.x, roomCenter.y - roomSize, 0);
            Vector3 upCorridor = new Vector3(roomCenter.x, roomCenter.y + roomSize, 0);
            Vector3[] array = {leftCorridor, rightCorridor, downCorridor, upCorridor};
            foreach (Vector3 cor in array) {
                int x = (int)cor.x;
                int y = (int)cor.y;
                if (0 <= x && x < dg.realMap.GetLength(0) && 0 <= y && y < dg.realMap.GetLength(1)) {
                    if (dg.realMap[x, y] != 0) {
                        neighbors.Add(roomCenter + 2f*(cor - roomCenter));
                    }
                }
            }
            return neighbors;
        }
    }
}