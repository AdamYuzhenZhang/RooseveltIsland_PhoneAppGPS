using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Simulator : MonoBehaviour
{
    public GPS_MQTT mqttConnector;
    private string messageOut;
    public Text messageOutText;
    
    public GameObject[] wayPoints;
    private int currentPt = 0;
    public Material pointMat;
    public Material pointMat_Activate;
    
    private float thisDistanceRatio;
    private float lastDistanceRatio;

    private float[] speeds = new float[13] {0.015f, 0.05f, 0.011f, 0.10f, 0.10f, 0.015f, 0.006f, 0.018f, 0.018f, 0.006f, 0.015f, 0.10f, 0.5f};
    
    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("updateInformation", 5f, 1f);
    }

    void updateInformation()
    {
        //getGPSInfo();
        // turn gps into message
        simulateMessage();
        publishToMQTT();
        thisDistanceRatio += speeds[currentPt];
        if (thisDistanceRatio >= 1)
        {
            nextPoint();
        }
    }

    private void simulateMessage()
    {
        messageOut = currentPt + " " + Mathf.Clamp(thisDistanceRatio, 0, 1) + " " + -1 + " " + -1;
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
        }
        
    }
    
    private void publishToMQTT()
    {
        mqttConnector.messagePublish = messageOut;
        mqttConnector.Publish();
    }
    
    public void restartSession()
    {
        SceneManager.LoadScene("GPS_Simulator");
    }
}
