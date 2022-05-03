using System.Collections.Generic;
using UnityEngine;
using UnityMovementAI;

namespace Generator
{
    public class UnitSpawner : MonoBehaviour
    {
        public GameObject guardUnit;
        public GameObject overseerUnit;
        public GameObject targetUnit;
        public GameObject playerUnit;

        [Header("Spawning Chance (they have to add up to at most 1)")]
        public float guardSpawnChance = 0.55f;
        public float overseerSpawnChance = 0.15f;
        public float targetSpawnChance = 0.1f;
        
        float guardSpawnThr;
        float overseerSpawnThr;
        float targetSpawnThr;

        [System.NonSerialized]
        public List<MovementAIRigidbody> GuardUnits = new List<MovementAIRigidbody>();
        [System.NonSerialized]
        public List<MovementAIRigidbody> OverseerUnits = new List<MovementAIRigidbody>();
        [System.NonSerialized]
        public List<MovementAIRigidbody> TargetUnits = new List<MovementAIRigidbody>();


        Transform guardTrans;
        Transform overseerTrans;
        Transform targetTrans;
        Transform playerTrans;
        float roomSize;
        List<Vector3> RoomCenters = new List<Vector3>();
        List<MovementAIRigidbody> Obstacles;
        
        // return the number of targets spawned
        public int Generate()
        {
            // remove all existing units and re-init units
            foreach (MovementAIRigidbody guard in GuardUnits) {
                Destroy(guard.gameObject);
            }
            GuardUnits = new List<MovementAIRigidbody>();
            foreach (MovementAIRigidbody overseer in OverseerUnits) {
                Destroy(overseer.gameObject);
            }
            OverseerUnits = new List<MovementAIRigidbody>();
            foreach (MovementAIRigidbody target in TargetUnits) {
                if (target != null) {
                    Destroy(target.gameObject);
                }
            }
            TargetUnits = new List<MovementAIRigidbody>();
            GameObject player = GameObject.Find("PlayerUnit(Clone)");
            if (player != null) {
                DestroyImmediate(player);
            }

            // get the room size, room positions, obstacles and the player
            DungeonGenerator dg = GameObject.Find("DungeonGenerator").GetComponent<DungeonGenerator>();
            roomSize = (float)dg.roomSize;
            RoomCenters = dg.Waypoints;
            Obstacles = GameObject.Find("ObstacleSpawner").GetComponent<ObstacleSpawner>().Objs;

            guardTrans = guardUnit.GetComponent<Transform>();
            overseerTrans = overseerUnit.GetComponent<Transform>();
            targetTrans = targetUnit.GetComponent<Transform>();
            playerTrans = playerUnit.GetComponent<Transform>();
            MovementAIRigidbody guardRb = guardTrans.GetComponent<MovementAIRigidbody>();
            MovementAIRigidbody overseerRb = overseerTrans.GetComponent<MovementAIRigidbody>();
            MovementAIRigidbody targetRb = targetTrans.GetComponent<MovementAIRigidbody>();
            MovementAIRigidbody playerRb = playerTrans.GetComponent<MovementAIRigidbody>();
            /* Manually set up the MovementAIRigidbody since the given obj can be a prefab */
            guardRb.SetUp();
            overseerRb.SetUp();
            targetRb.SetUp();

            // calculate chance threshould
            guardSpawnThr = guardSpawnChance;
            overseerSpawnThr = guardSpawnThr + overseerSpawnChance;
            targetSpawnThr = overseerSpawnThr + targetSpawnChance;

            /* Create the objects in each room*/
            Transform obj = null;
            List<MovementAIRigidbody> list = null;
            int i = 0;
            foreach (Vector3 roomCenter in RoomCenters) {
                // place the player
                if (i == 0) {
                    while (!TryToCreateObject(roomCenter, playerTrans, null)) {}
                // spawn a guard
                } else if (i == 1) {
                    while (!TryToCreateObject(roomCenter, guardTrans, GuardUnits)) {}
                // spawn a target
                } else if (i == RoomCenters.Count - 1) {
                    while (!TryToCreateObject(roomCenter, targetTrans, TargetUnits)) {}
                } else {
                    float rand = Random.Range(0f, 1f);
                    if (rand <= guardSpawnChance) {
                        obj = guardTrans;
                        list = GuardUnits;
                    } else if (rand <=overseerSpawnThr) {
                        obj = overseerTrans;
                        list = OverseerUnits;
                    } else if (rand <= targetSpawnThr) {
                        obj = targetTrans;
                        list = TargetUnits;
                    } else {
                        i++;
                        continue;
                    }
                    /* Try to place the objects multiple times before giving up */
                    for (int j = 0; j < 10; j++) {
                        if (TryToCreateObject(roomCenter, obj, list)) {
                            break;
                        }
                    }
                }
                i++;
            }
            return TargetUnits.Count;
        }

        bool TryToCreateObject(Vector3 roomCenter, Transform obj, List<MovementAIRigidbody> list)
        {
            float halfSize = 0.7f/2f;

            // calculate the window to put units in a room around the center
            float halfWindowSize = roomSize/2f - halfSize;

            float left = roomCenter.x - halfWindowSize;
            float right = roomCenter.x + halfWindowSize;
            float bottom = roomCenter.y - halfWindowSize;
            float top = roomCenter.y + halfWindowSize;

            // spawn a random position and check for availability
            Vector3 pos = new Vector3(Random.Range(left, right), Random.Range(bottom, top), 0f);

            if (CanPlaceObject(halfSize, pos))
            {
                Transform t = Instantiate(obj, pos, Quaternion.identity) as Transform;

                t.localScale = new Vector3(1f, 1f, obj.localScale.z);

                if (list != null) {
                    list.Add(t.GetComponent<MovementAIRigidbody>());
                }

                return true;
            }

            return false;
        }

        bool CanPlaceObject(float halfSize, Vector3 pos)
        {
            /* Make sure it does not overlap with any existing object */
            foreach (MovementAIRigidbody o in Obstacles)
            {
                float dist = Vector3.Distance(o.Position, pos);

                if (dist < o.Radius + halfSize)
                {
                    return false;
                }
            }

            return true;
        }
    }
}