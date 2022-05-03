using System.Collections.Generic;
using UnityEngine;
using UnityMovementAI;

namespace Generator
{
    public class DungeonGenerator: MonoBehaviour
    {
        public GameObject wall;
        Transform trans;
        public int width = 5;
        public int height = 5;
        public int roomSize = 5;
        public int minWalkLen = 5;
        public int maxWalkLen = 12;
        public float minorRoomChance = 0.3f;

        int wrapWidth;
        int middle;

        [System.NonSerialized]
        public List<MovementAIRigidbody> Walls = new List<MovementAIRigidbody>();
        // waypoints for room centers
        public List<Vector3> Waypoints;
        // higher resolution waypoints including corridors
        public List<Vector3> HRWaypoints;
        // navigation graph
        public Dictionary<Vector3, List<Vector3>> Neighbors;

        [System.NonSerialized]
        public int[,] realMap;

        void Start() {
            wrapWidth = (roomSize + 1)/2;
            middle = roomSize/2;
        }

        public void Generate() {
            trans = wall.GetComponent<Transform>();
            MovementAIRigidbody rb = trans.GetComponent<MovementAIRigidbody>();
            /* Manually set up the MovementAIRigidbody since the given obj can be a prefab */
            rb.SetUp();

            // remove all existing walls and re-init Walls and Waypoints
            foreach (MovementAIRigidbody wall in Walls) {
                Destroy(wall.gameObject);
            }
            Walls = new List<MovementAIRigidbody>();
            Waypoints = new List<Vector3>();
            HRWaypoints = new List<Vector3>();
            Neighbors = new Dictionary<Vector3, List<Vector3>>();
            realMap = new int[(width*2 - 1)*roomSize + 2*wrapWidth, (height*2 - 1)*roomSize + 2*wrapWidth];

            // build cyclic dungeon
            int[, ] map = InitCycle(width, height, Random.Range(minWalkLen, maxWalkLen + 1));
            map = Expanded(map);
            map = Polished(map);
            PlaceWalls(map);
        }

        // initialize the cycle on a small grid
        int[,] InitCycle(int width, int height, int walkLen) {

            // the idea is that given a grid where the cross points are the nodes (like a Go board)
            // walk on the cells instead of the cross points, and trace the path on the expanded grid to get a cycle
            bool[, ] cellMap = new bool[width - 1, height - 1];

            // use this array to trace the walk, every time a cell is visited, increase the counter for the adjacent cross points by 1
            // the cross points ajacent to 1, 2, or 3 visited cells become a part of the cycle
            // and the grid is exanded so the edges between the cross points also become nodes
            int[, ] expandedMap = new int[width*2 - 1, height*2 - 1];

            // possible directions
            Vector2Int upInt = new Vector2Int(0, 1);
            Vector2Int downInt = new Vector2Int(0, -1);
            Vector2Int leftInt = new Vector2Int(-1, 0);
            Vector2Int rightInt = new Vector2Int(1, 0);

            // choose a random start
            Vector2Int curr = new Vector2Int(Random.Range(0, width - 1), Random.Range(0, height - 1));
            // visit the starting node
            Visit(cellMap, expandedMap, curr);

            // start a walking of length walkLen
            for (int i = 1; i < walkLen; i++) {
                // walk one step randomly

                // get all possible destinations
                Vector2Int upDest = curr + upInt;
                Vector2Int downDest = curr + downInt;
                Vector2Int leftDest = curr + leftInt;
                Vector2Int rightDest = curr + rightInt;
                Vector2Int[] dest =  {upDest, downDest, leftDest, rightDest};

                // get all the valid destinations
                Vector2Int[] validDest =  new Vector2Int[4];
                int validCnt = 0;
                for (int j = 0; j < dest.GetLength(0); j++) {
                    if (IsValidDestination(cellMap, dest[j])) {
                        validDest[validCnt++] = dest[j];
                    }
                }

                // choose one destination randomly, if any
                if (validCnt == 0) break;
                int sample1 = Random.Range(0, validCnt);
                Vector2Int next = validDest[sample1];

                // visit the destination node
                Visit(cellMap, expandedMap, next);

                // maybe add a minor room
                if (validCnt > 1 && Random.Range(0f, 1f) <= minorRoomChance) {
                    int sample2 = Random.Range(0, validCnt - 1);
                    if (sample2 >= sample1) {
                        sample2++;
                    }
                    Vector2Int randomRoom = validDest[sample2];
                    Visit(cellMap, expandedMap, randomRoom);
                }

                // break edges as we walk
                Vector2Int edgePos;
                if (next == upDest) {
                    edgePos = new Vector2Int(next.x*2 + 1, next.y*2);
                } else if (next == downDest) {
                    edgePos = new Vector2Int(next.x*2 + 1, (next.y + 1)*2);
                } else if (next == leftDest) {
                    edgePos = new Vector2Int((next.x + 1)*2, next.y*2 + 1);
                } else {
                    edgePos = new Vector2Int(next.x*2, next.y*2 + 1);
                }
                expandedMap[edgePos.x, edgePos.y]=-10;

                curr = next;
            }

            // trace the cycle and build edges
            int halfEdge = 5;
            int edge = 2*halfEdge;
            for (int x = 0; x < expandedMap.GetLength(0); x++) {
                for (int y = 0; y < expandedMap.GetLength(1); y++) {
                    Vector2Int pos = new  Vector2Int(x, y);
                    if (0 < expandedMap[x, y] && expandedMap[x, y] < halfEdge) {
                        // add half edges
                        Vector2Int adjacent;
                        adjacent = pos + upInt;
                        if (0 <= adjacent.x && adjacent.x < expandedMap.GetLength(0) && 0 <= adjacent.y && adjacent.y < expandedMap.GetLength(1)) {
                            expandedMap[adjacent.x, adjacent.y] += halfEdge;
                        }
                        adjacent = pos + downInt;
                        if (0 <= adjacent.x && adjacent.x < expandedMap.GetLength(0) && 0 <= adjacent.y && adjacent.y < expandedMap.GetLength(1)) {
                            expandedMap[adjacent.x, adjacent.y] += halfEdge;
                        }
                        adjacent = pos + leftInt;
                        if (0 <= adjacent.x && adjacent.x < expandedMap.GetLength(0) && 0 <= adjacent.y && adjacent.y < expandedMap.GetLength(1)) {
                            expandedMap[adjacent.x, adjacent.y] += halfEdge;
                        }
                        adjacent = pos + rightInt;
                        if (0 <= adjacent.x && adjacent.x < expandedMap.GetLength(0) && 0 <= adjacent.y && adjacent.y < expandedMap.GetLength(1)) {
                            expandedMap[adjacent.x, adjacent.y] += halfEdge;
                        }
                    }
                }
            }

            // assign 1 to rooms and 2 to corridors, and 0 to the rest; build waypoints
            float roomHalfSize = (float)((roomSize - 1)/2);
            Vector3 left = new Vector3(-roomHalfSize, 0, 0);
            Vector3 right = new Vector3(roomHalfSize, 0, 0);
            Vector3 down = new Vector3(0, -roomHalfSize, 0);
            Vector3 up = new Vector3(0, roomHalfSize, 0);
            for (int x = 0; x < expandedMap.GetLength(0); x++) {
                for (int y = 0; y < expandedMap.GetLength(1); y++) {
                    if (expandedMap[x, y] == edge) {
                        expandedMap[x, y] = 2;
                    } else if (0 < expandedMap[x, y] && expandedMap[x, y] < halfEdge) {
                        expandedMap[x, y] = 1;
                        // calculate the room's corrdinate on the final map
                        float real_x = (float)x*roomSize + middle + wrapWidth;
                        float real_y = (float)y*roomSize + middle + wrapWidth;
                        Vector3 roomCenter = new Vector3(real_x, real_y, 0);
                        Waypoints.Add(roomCenter);
                        HRWaypoints.Add(roomCenter);
                        // add corridors
                        Vector3 leftCorridor = roomCenter + left;
                        Vector3 rightCorridor = roomCenter + right;
                        Vector3 downCorridor = roomCenter + down;
                        Vector3 upCorridor = roomCenter + up;
                        HRWaypoints.Add(leftCorridor);
                        HRWaypoints.Add(rightCorridor);
                        HRWaypoints.Add(downCorridor);
                        HRWaypoints.Add(upCorridor);
                        // build graph
                        Neighbors[roomCenter] = new List<Vector3>();
                        Neighbors[leftCorridor] = new List<Vector3>();
                        Neighbors[rightCorridor] = new List<Vector3>();
                        Neighbors[downCorridor] = new List<Vector3>();
                        Neighbors[upCorridor] = new List<Vector3>();
                        Neighbors[roomCenter].Add(leftCorridor);
                        Neighbors[leftCorridor].Add(roomCenter);
                        Neighbors[roomCenter].Add(rightCorridor);
                        Neighbors[rightCorridor].Add(roomCenter);
                        Neighbors[roomCenter].Add(downCorridor);
                        Neighbors[downCorridor].Add(roomCenter);
                        Neighbors[roomCenter].Add(upCorridor);
                        Neighbors[upCorridor].Add(roomCenter);
                    } else {
                        expandedMap[x, y] = 0;
                    }
                }
            }
            // go through the map again to connect corridors
            Vector3 left1 = new Vector3(-1, 0, 0);
            Vector3 right1 = new Vector3(1, 0, 0);
            Vector3 down1 = new Vector3(0, -1, 0);
            Vector3 up1 = new Vector3(0, 1, 0);
            for (int x = 0; x < expandedMap.GetLength(0); x++) {
                for (int y = 0; y < expandedMap.GetLength(1); y++) {
                    if (expandedMap[x, y] == 2) {
                        // vertical edges
                        if (x%2 == 0) {
                            float real_x = (float)x*roomSize + middle + wrapWidth;
                            float real_yDown = (float)y*roomSize + wrapWidth;
                            Vector3 pointDown = new Vector3(real_x, real_yDown);
                            float real_yUp = real_yDown + (float)roomSize - 1;
                            Vector3 pointUp = new Vector3(real_x, real_yUp);
                            // add waypoints
                            HRWaypoints.Add(pointDown);
                            HRWaypoints.Add(pointUp);
                            // buid graph
                            Neighbors[pointDown] = new List<Vector3>();
                            Neighbors[pointDown].Add(pointUp);
                            Neighbors[pointUp] = new List<Vector3>();
                            Neighbors[pointUp].Add(pointDown);
                            // find exit
                            Vector3 downOut = pointDown + down1;
                            Vector3 upOut = pointUp + up1;
                            // if the rooms exist, connect them with the corridor
                            if (Neighbors.ContainsKey(downOut)) {
                                Neighbors[downOut].Add(pointDown);
                                Neighbors[pointDown].Add(downOut);
                            }
                            if (Neighbors.ContainsKey(upOut)) {
                                Neighbors[upOut].Add(pointUp);
                                Neighbors[pointUp].Add(upOut);
                            }
                        // horizontal edges
                        } else {
                            float real_xLeft = (float)x*roomSize + wrapWidth;
                            float real_y = (float)y*roomSize + middle + wrapWidth;
                            Vector3 pointLeft = new Vector3(real_xLeft, real_y);
                            float real_xRight = real_xLeft + (float)roomSize - 1;
                            Vector3 pointRight = new Vector3(real_xRight, real_y);
                            // add waypoints
                            HRWaypoints.Add(pointLeft);
                            HRWaypoints.Add(pointRight);
                            // buid graph
                            Neighbors[pointLeft] = new List<Vector3>();
                            Neighbors[pointLeft].Add(pointRight);
                            Neighbors[pointRight] = new List<Vector3>();
                            Neighbors[pointRight].Add(pointLeft);
                            // find exit
                            Vector3 leftOut = pointLeft + left1;
                            Vector3 rightOut = pointRight + right1;
                            // if the rooms exist, connect them with the corridor
                            if (Neighbors.ContainsKey(leftOut)) {
                                Neighbors[leftOut].Add(pointLeft);
                                Neighbors[pointLeft].Add(leftOut);
                            }
                            if (Neighbors.ContainsKey(rightOut)) {
                                Neighbors[rightOut].Add(pointRight);
                                Neighbors[pointRight].Add(rightOut);
                            }
                        }
                    }
                }
            }

            // shuffle the rooms
            Shuffle(Waypoints);

            return expandedMap;
        }

        // check the position is a visitable node on the graph (not out of bounds and not visited)
        bool IsValidDestination(bool[, ] map, Vector2Int pos) {
            int x = pos.x;
            int y = pos.y;
            return 0 <= x && x < map.GetLength(0) && 0 <= y && y < map.GetLength(1) && !map[x, y];
        }

        // visit the destination (next) node on the cell map
        void Visit(bool[, ] cellMap, int[, ] expandedMap, Vector2Int next) {
            cellMap[next.x, next.y] = true;

            // increase count of visited adjacent cells for the cross points (four corners)
            expandedMap[next.x*2, next.y*2]++; // bottom-left
            expandedMap[next.x*2, (next.y + 1)*2]++; // upper-left
            expandedMap[(next.x + 1)*2, next.y*2]++; // bottom-right
            expandedMap[(next.x + 1)*2, (next.y + 1)*2]++; // upper-right
        }

        // expand the grid even more so it is in its actual size
        int[, ] Expanded(int[, ] expandedMap) {
            for (int x = wrapWidth; x < realMap.GetLength(0) - wrapWidth; x++) {
                int x_unwrapped = x - wrapWidth;
                int x_small = x_unwrapped/roomSize;
                for (int y = wrapWidth; y < realMap.GetLength(1) - wrapWidth; y++) {
                    int y_unwrapped = y - wrapWidth;
                    int y_small = y_unwrapped/roomSize;
                    int value = expandedMap[x_small, y_small];
                    // if it is an edge
                    if (value == 2) {
                        // vertical edges
                        if (x_small%2 == 0) {
                            if (x_unwrapped%roomSize == middle) {
                                realMap[x, y] = 2;
                            }
                        // horizontal edges
                        } else {
                            if (y_unwrapped%roomSize == middle) {
                                realMap[x, y] = 2;
                            }
                        }
                    // not an edge, copy value
                    } else {
                        realMap[x, y] = value;
                    }
                }
            }
            return realMap;
        }

        int[, ] Polished(int[, ] map) {
            // dig the walls so they look more random
            int windowSize = roomSize - 2;
            int maxExpand = (roomSize - 1)/2 + 1;
            for (int i = 0; i < maxExpand; i++) {
                // copy the original map
                int[,] copy = new int[map.GetLength(0), map.GetLength(1)];
                for (int x = 0; x < map.GetLength(0); x++) {
                    for (int y = 0; y < map.GetLength(1); y++) {
                        copy[x, y] = map[x, y];
                    }
                }
                // polish the rooms
                foreach (Vector3 roomCenter in Waypoints) {
                    int left = (int)roomCenter.x - windowSize;
                    int right = (int)roomCenter.x + windowSize;
                    int bottom = (int)roomCenter.y - windowSize;
                    int top = (int)roomCenter.y + windowSize;
                    for (int x = left; x <= right; x++) {
                        for (int y = bottom; y <= top; y++) {
                            if (copy[x, y] != 0) {
                                // expand with some chance
                                if (Random.Range(0f, 1f) <= 0.2f) {
                                    map[x - 1, y] = 1;
                                    map[x + 1, y] = 1;
                                    map[x, y - 1] = 1;
                                    map[x, y + 1] = 1;
                                    map[x - 1, y - 1] = 1;
                                    map[x + 1, y - 1] = 1;
                                    map[x - 1, y + 1] = 1;
                                    map[x + 1, y + 1] = 1;
                                }
                            }
                        }
                    }
                }
            }
            return map;
        }

        void PlaceWalls(int[, ] map) {
            for (int x = 0; x < map.GetLength(0); x++) {
                for (int y = 0; y < map.GetLength(1); y++) {
                    if (map[x, y] == 0) {
                        Vector3 pos = new Vector3();
                        pos.x = (float)x;
                        pos.y = (float)y;

                        Transform t = Instantiate(trans, pos, Quaternion.identity) as Transform;
                        Walls.Add(t.GetComponent<MovementAIRigidbody>());
                    }
                }
            }
        }

        static void Shuffle<T>(List<T> list) {  
            for (int i = 0; i < list.Count - 1; i++) {
                int j = Random.Range(i, list.Count);
                T value = list[i];
                list[i] = list[j];
                list[j] = value;
            }
        }
    }
}