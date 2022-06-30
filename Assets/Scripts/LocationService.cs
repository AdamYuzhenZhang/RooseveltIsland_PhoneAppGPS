using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class LocationService : MonoBehaviour
{
    public Text gpsOut;
    public bool isUpdating;
    private bool serviceStarted;
    public string gpsMessageOut;

    public Vector3 gpsLocation = new Vector3();
    public double timeStamp = 0;

    private void Start()
    {
        StartCoroutine(StartLocationService());
    }

    private void Update()
    {
        if (serviceStarted)
        {
            //StartCoroutine(GetLocation());
            GetLocation();
            //isUpdating = !isUpdating;
        }
    }

    IEnumerator StartLocationService()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            Permission.RequestUserPermission(Permission.CoarseLocation);
        }

        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
            yield return new WaitForSeconds(10);

        // Start service before querying location
        Input.location.Start(1, 0.1f);

        // Wait until service initializes
        int maxWait = 10;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            gpsOut.text = "Timed out";
            print("Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            gpsOut.text = "Unable to determine device location";
            print("Unable to determine device location");
            yield break;
        }
        else
        {
            serviceStarted = true;
        }
    }

    void GetLocation()
    {
        gpsOut.text = "Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " +
                      Input.location.lastData.altitude + 100f + " " + Input.location.lastData.horizontalAccuracy + " " +
                      Input.location.lastData.timestamp;
        // Access granted and location value could be retrieved
        print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " +
              Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " +
              Input.location.lastData.timestamp);

        gpsLocation = new Vector3(Input.location.lastData.latitude, 0, Input.location.lastData.longitude);
        timeStamp = Input.location.lastData.timestamp;
    }
}

