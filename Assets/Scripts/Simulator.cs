using System;
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

    private float[] speeds = new float[13] {0.015f, 0.05f, 0.011f, 0.10f, 0.10f, 0.003f, 0.006f, 0.018f, 0.018f, 0.006f, 0.015f, 0.10f, 0.5f};
    private float[] speedsFast = new float[13] {0.03f, 0.08f, 0.02f, 0.2f, 0.2f, 0.006f, 0.01f, 0.018f, 0.018f, 0.006f, 0.015f, 0.10f, 0.5f};

    public bool faster;
    public bool shouldslow;
    private int m_currentMessageValue = 0;
    
    [SerializeField] 
    private float deltaTime;

    [SerializeField] 
    private float sleepTime;

    [SerializeField] 
    private bool _shouldFast; // whether the bus should accelerate at the later part

    private bool isWake = false;
    private bool isStart = false;
    private bool atFirefighter;
    
    // Start is called before the first frame update
    void Start()
    {
        for (int i =0; i<speeds.Length;i++)
        {
            speeds[i] = speeds[i] * 1.1f;
        }
        if (shouldslow)
        {
            for (int i =0; i<speeds.Length;i++)
            {
                speeds[i] = speeds[i] * 0.75f;
            }
        }
        // whether the bus should accelerate at the later part, control by user
        if (_shouldFast)
        {
            if (faster)
            {
                speeds = speedsFast;
            }
        }
        StartCoroutine(WaitforSecs());
        Time.fixedDeltaTime = deltaTime;
    }

    private void FixedUpdate()
    {
        if (isWake)
        {
            if (isStart)
            {
                updateInformation();
            }
        }
    }

    void updateInformation()
    {
        //getGPSInfo();
        // turn gps into message
        if (!atFirefighter)
        {
            simulateMessage();
            publishToMQTT();
            
            thisDistanceRatio += speeds[currentPt];
            if (thisDistanceRatio >= 1)
            {
                nextPoint();
            }
        }
        
    }

    private void simulateMessage()
    {
        messageOut = currentPt + " " + Mathf.Clamp(thisDistanceRatio, 0, 1) + " " + -1 + " " + -1;
        switch (m_currentMessageValue)
        {
            case 0:
                // normal
                messageOut = currentPt + " " + Mathf.Clamp(thisDistanceRatio, 0, 1) + " " + -1 + " " + -1;
                break;
            case -1:
                // reset
                messageOut = currentPt + " " + Mathf.Clamp(thisDistanceRatio, 0, 1) + " -10 -10";
                break;
            case 1:
                // firefighter start
                messageOut = currentPt + " " + Mathf.Clamp(thisDistanceRatio, 0, 1) + " -5 -5";
                break;
            case 2:
                // firefighter end
                messageOut = currentPt + " " + Mathf.Clamp(thisDistanceRatio, 0, 1) + " -6 -6";
                break;
            case 3:
                // start video
                messageOut = currentPt + " " + Mathf.Clamp(thisDistanceRatio, 0, 1) + " -50 -50";
                break;
            case 4:
                // black out video
                messageOut = currentPt + " " + Mathf.Clamp(thisDistanceRatio, 0, 1) + " -7 -7";
                break;
            case 5:
                // unblock the video
                messageOut = currentPt + " " + Mathf.Clamp(thisDistanceRatio, 0, 1) + " -8 -8";
                break;
        }
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
    public void resetHeadsets()
    {
        m_currentMessageValue = -1;
        simulateMessage();
    }
    public void FirefighterStart()
    {
        m_currentMessageValue = 1;
        simulateMessage();
        publishToMQTT();
        atFirefighter = true;
    }
    public void FirefighterEnd()
    {
        atFirefighter = false;
        m_currentMessageValue = 2;
        simulateMessage();
    }
    public void StartVideo()
    {
        // Will not activate until press start video
        mqttConnector.Connect();
        isStart = true;
        m_currentMessageValue = 3;
        simulateMessage();
    }

    // method to make the screen black.
    public void BlackOut()
    {
        m_currentMessageValue = 4;
        simulateMessage();
    }
    
    // method to resume from black scree
    public void ResumeBlackout()
    {
        m_currentMessageValue = 5;
        simulateMessage();
    }
    private void publishToMQTT()
    {
        mqttConnector.messagePublish = messageOut;
        mqttConnector.Publish();
        m_currentMessageValue = 0;
    }
    
    public void restartSession()
    {
        SceneManager.LoadScene("GPS_Simulator");
    }
    
    IEnumerator WaitforSecs()
    {
        yield return new WaitForSeconds(sleepTime);
        isWake = true;
    }
}
