using UnityEngine;
using UnityEngine.UI;
using UnityMovementAI;
using Generator;
using Agent;

namespace Management
{
    public class GameManager : MonoBehaviour
    {
        DungeonGenerator dg;
        ObstacleSpawner os;
        UnitSpawner us;
        CameraManager cm;
        Text win;

        [System.NonSerialized]
        public Vector3 alertPosition;
        [System.NonSerialized]
        public Quaternion alertQuaternion;
        [System.NonSerialized]
        // this is a reserved position for unspecified alert position as Vector3 is not nullable
        public Vector3 nullAlert = new Vector3(0, 0, 0);

        public int numTargets;
        public bool gameWon;
        public bool running;

        public GameObject Dummy;
        Transform dummyTrans;

        [System.NonSerialized]
        public Transform dummyInstance = null;

        void Start() {
            running = false;
            win = GameObject.Find("Win").GetComponent<Text>();
            win.color = new Color(0f, 0f, 0f, 0f);
            dg = GameObject.Find("DungeonGenerator").GetComponent<DungeonGenerator>();
            os = GameObject.Find("ObstacleSpawner").GetComponent<ObstacleSpawner>();
            us = GameObject.Find("UnitSpawner").GetComponent<UnitSpawner>();
            cm = GameObject.Find("Main Camera").GetComponent<CameraManager>();
            dummyTrans = Dummy.GetComponent<Transform>();
        }

        public void Generate() {
            running = true;
            gameWon = false;
            win.color = new Color(0f, 0f, 0f, 0f);
            dg.Generate();
            os.Generate();
            numTargets = us.Generate();
            cm.FindPlayer();
            alertPosition = nullAlert;
            if (dummyInstance != null) {
                Destroy(dummyInstance.gameObject);
            }
        }

        void FixedUpdate() {
            if (!running) return;
            if (!gameWon && numTargets == 0) {
                foreach (MovementAIRigidbody guard in us.GuardUnits) {
                    guard.GetComponent<GuardUnit>().target = null;
                }
                gameWon = true;
                running = false;
                win.color = new Color(30f/255f, 140f/255f, 110f/255f, 1f);
            }
            // update alert position
            if (alertPosition != nullAlert) {
                if (dummyInstance == null) {
                    dummyInstance = Instantiate(dummyTrans, alertPosition, alertQuaternion) as Transform;
                }
                dummyInstance.position = alertPosition;
                dummyInstance.rotation = alertQuaternion;
            }
        }
    }
}