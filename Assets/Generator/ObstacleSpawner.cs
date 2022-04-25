using System.Collections.Generic;
using UnityEngine;
using UnityMovementAI;

namespace Generator
{
    public class ObstacleSpawner : MonoBehaviour
    {
        public GameObject ob;
        public Vector2 objectSizeRange = new Vector2(0.5f, 0.8f);

        public int maxNumberOfObjectsInARoom = 4;

        public float spaceBetweenObjects = 1f;

        [System.NonSerialized]
        public List<MovementAIRigidbody> Objs = new List<MovementAIRigidbody>();

        Transform obj;
        float roomSize;
        List<Vector3> RoomCenters = new List<Vector3>();

        public void Generate()
        {
            // remove all existing obstacles and re-init Objs
            foreach (MovementAIRigidbody obstacle in Objs) {
                Destroy(obstacle.gameObject);
            }
            Objs = new List<MovementAIRigidbody>();

            // get the room size and room positions from the dungeon generator
            DungeonGenerator dg = GameObject.Find("DungeonGenerator").GetComponent<DungeonGenerator>();
            roomSize = (float)dg.roomSize;
            RoomCenters = dg.Waypoints;

            obj = ob.GetComponent<Transform>();
            MovementAIRigidbody rb = obj.GetComponent<MovementAIRigidbody>();
            /* Manually set up the MovementAIRigidbody since the given obj can be a prefab */
            rb.SetUp();

            /* Create the objects in each room*/
            foreach (Vector3 roomCenter in RoomCenters) {
                for (int i = 0; i < maxNumberOfObjectsInARoom; i++) {
                    /* Try to place the objects multiple times before giving up */
                    for (int j = 0; j < 10; j++) {
                        if (TryToCreateObject(roomCenter)) {
                            break;
                        }
                    }
                }
            }
        }

        bool TryToCreateObject(Vector3 roomCenter)
        {
            float size = Random.Range(objectSizeRange.x, objectSizeRange.y);
            float halfSize = size / 2f;

            // calculate the window to put objects in a room around the center
            float halfWindowSize = roomSize/2f - halfSize - spaceBetweenObjects;

            float left = roomCenter.x - halfWindowSize;
            float right = roomCenter.x + halfWindowSize;
            float bottom = roomCenter.y - halfWindowSize;
            float top = roomCenter.y + halfWindowSize;

            // spawn a random position and check for availability
            Vector3 pos = new Vector3(Random.Range(left, right), Random.Range(bottom, top), 0f);

            if (CanPlaceObject(halfSize, pos, roomCenter))
            {
                Transform t = Instantiate(obj, pos, Quaternion.identity) as Transform;

                t.localScale = new Vector3(size, size, obj.localScale.z);

                Objs.Add(t.GetComponent<MovementAIRigidbody>());

                return true;
            }

            return false;
        }

        bool CanPlaceObject(float halfSize, Vector3 pos, Vector3 waypoint)
        {
            /* Make sure it does not overlap with any existing object or waypoints */
            
            float dist = Vector3.Distance(waypoint, pos);

            if (dist < 0.4 + halfSize)
            {
                return false;
            }

            foreach (MovementAIRigidbody o in Objs)
            {
                dist = Vector3.Distance(o.Position, pos);

                if (dist < o.Radius + spaceBetweenObjects + halfSize)
                {
                    return false;
                }
            }

            return true;
        }
    }
}