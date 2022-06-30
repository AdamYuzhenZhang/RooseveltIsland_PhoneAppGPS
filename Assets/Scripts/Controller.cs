using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Controller : MonoBehaviour
{
    public GPS_MQTT mqttConnector;
    public LocationService gpsLocation;

    private Vector3 gpsLocationdata = new Vector3();
    private double timeStamp = 0;
    private string messageOut;

    private Vector3 originGPS = new Vector3(40.76402f, 0f, -73.94875f);
    private float longToX = 40000f;
    private float latToZ = 54400f;

    private float pathTotalLength = 2373.904f;
    public GameObject[] wayPoints;
    private int currentPt = 0;


    public CinemachineSmoothPath dollyTrack;
    public GameObject playerPosition;
    public GameObject simulatroPosition;

    private float thisDistanceRatio;
    private float lastDistanceRatio;
    private float simulatedRatio;
    private double lastTimeStamp;
    
    public Text messageOutText;

    public Material pointMat;
    public Material pointMat_Activate;
    
    private float[] speeds = new float[13] {0.02f, 0.06f, 0.013f, 0.12f, 0.12f, 0.02f, 0.008f, 0.022f, 0.022f, 0.008f, 0.02f, 0.12f, 0.6f};

    private bool isMoving;
    
    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        InvokeRepeating("updateInformation", 5f, 1f);
    }

    private void Update()
    {
        //Debug.Log("pathLength");
        //Debug.Log(dollyTrack.PathLength);
        //Debug.Log(dollyTrack.ToNativePathUnits(500, CinemachinePathBase.PositionUnits.Distance));
    }

    void updateInformation()
    {
        getGPSInfo();
        // turn gps into message
        gpsToMessage(0);
        publishToMQTT();

        if (isMoving)
        {
            simulatedRatio += speeds[currentPt];
            setSimulatorPosition();
        }
    }

    public void toggleIsMoving()
    {
        isMoving = !isMoving;
    }
    
    // turn gps into message
    private void gpsToMessage(int command)
    {
        // turn gps location into virtual world location
        Vector3 worldLocation = deltaGPStoWorldCoord(deltaGPS());
        playerPosition.transform.position = worldLocation;
        // find the nearest point along path to the gps point in world coordinate
        // find the path location of that point
        lastDistanceRatio = thisDistanceRatio;
        thisDistanceRatio = distanceRatio(worldLocation);
        float deltaRatio = deltaDistanceRatio();
        float deltaTime = deltaTimeStamp();

        float weightedRatio = thisDistanceRatio;
        
        // automatic adding
        if (weightedRatio > 0.97)
        {
            nextPoint();
        }
        
        // To String
        //messageOut = currentPt + " " + simulatedRatio + " " + deltaRatio + " " + deltaTime;
        switch (command)
        {
            case 0:
                messageOut = currentPt + " " + weightedRatio + " " + deltaRatio + " " + deltaTime;
                break;
            case -1:
                messageOut = currentPt + " " + weightedRatio + " -10 -10";
                break;
            case 1:
                messageOut = currentPt + " " + weightedRatio + " -5 -5";
                break;
            case 2:
                messageOut = currentPt + " " + weightedRatio + " -6 -6";
                break;
        }
        // debug message
        messageOutText.text = messageOut;
    }

    public void nextPoint()
    {
        if (currentPt < wayPoints.Length - 2)
        {
            currentPt += 1;
            foreach (GameObject pt in wayPoints)
            {
                pt.GetComponent<MeshRenderer>().material = pointMat;
            }

            wayPoints[currentPt].GetComponent<MeshRenderer>().material = pointMat_Activate;
            wayPoints[currentPt + 1].GetComponent<MeshRenderer>().material = pointMat_Activate;

            lastDistanceRatio = 0;
            thisDistanceRatio = 0;
            simulatedRatio = 0;
        }
        
    }

    public void restartSession()
    {
        SceneManager.LoadScene("GPS");
    }
    public void resetHeadsets()
    {
        gpsToMessage(-1);
    }
    public void FirefighterStart()
    {
        gpsToMessage(1);
    }
    public void FirefighterEnd()
    {
        gpsToMessage(2);
    }

    // For finite lines:
    Vector3 GetClosestPointOnFiniteLine(Vector3 point, Vector3 line_start, Vector3 line_end)
    {
        Vector3 line_direction = line_end - line_start;
        float line_length = line_direction.magnitude;
        line_direction.Normalize();
        float project_length = Mathf.Clamp(Vector3.Dot(point - line_start, line_direction), 0f, line_length);
        return line_start + line_direction * project_length;
    }

    private float distanceRatio(Vector3 worldLocation)
    {
        Vector3 ptOnLine = GetClosestPointOnFiniteLine(worldLocation, wayPoints[currentPt].transform.position,
            wayPoints[currentPt + 1].transform.position);
        float distance = Vector3.Distance(ptOnLine, wayPoints[currentPt].transform.position) /
               Vector3.Distance(wayPoints[currentPt + 1].transform.position, wayPoints[currentPt].transform.position);
        return Mathf.Clamp(distance, 0, 1);
    }

    private float deltaDistanceRatio()
    {
        return (thisDistanceRatio - lastDistanceRatio);
    }

    private void setSimulatorPosition()
    {
        Vector3 pt1 = wayPoints[currentPt].transform.position;
        Vector3 pt2 = wayPoints[currentPt+1].transform.position;
        Vector3 position = pt1 + (pt2 - pt1) * simulatedRatio;
        simulatroPosition.transform.position = position;
    }
    private float deltaTimeStamp()
    {
        return (float)(timeStamp - lastTimeStamp);
    }

    // gps difference between point and world origin
    private Vector3 deltaGPS()
    {
        return originGPS - gpsLocationdata;
    }

    private Vector3 deltaGPStoWorldCoord(Vector3 deltaGPS)
    {
        return new Vector3(deltaGPS.z * longToX, 0f, deltaGPS.x * latToZ);
    }

    private void getGPSInfo()
    {
        gpsLocationdata = gpsLocation.gpsLocation;
        lastTimeStamp = timeStamp;
        timeStamp = gpsLocation.timeStamp;
    }

    private void publishToMQTT()
    {
        mqttConnector.messagePublish = messageOut;
        mqttConnector.Publish();
    }
}
