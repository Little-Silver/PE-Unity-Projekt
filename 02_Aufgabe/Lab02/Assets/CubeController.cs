using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System;

/*
    Accelerates the cube to which it is attached, modelling an harmonic oscillator.
    Writes the position, velocity and acceleration of the cube to a CSV file.
    
    Remark: For use in "Physics Engines" module at ZHAW, part of physics lab
    Author: kemf
    Version: 1.0
*/
public class CubeController : MonoBehaviour
{
    private Rigidbody rigidBody;

    public int springConstant; // N/m

    private float currentTimeStep; // s
    
    private List<List<float>> timeSeries;

    // Start is called before the first frame update
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        timeSeries = new List<List<float>>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private float featherPosXFrom = 0; // m
    private double featherPosXTo = -3.5; // m
    private bool featherEnabled = false;
    private bool setInitialForce = false;
    
    // FixedUpdate can be called multiple times per frame
    void FixedUpdate() {
        float forceX = 0; // N

        rigidBody.mass = 1; // kg
        springConstant = 1;  // N/m
        
        // Assuming the Feather starts at position x = 0
        float compression = rigidBody.position.x;

        //Set Initial Force before Feather is touched
        if (!setInitialForce)
        {
            //Apply Force
            forceX = -50; // N
            rigidBody.AddForce(new Vector3(forceX, 0f, 0f));
            setInitialForce = true;
        }

        //Pre-Feather
        if (rigidBody.position.x < featherPosXFrom && rigidBody.position.x > featherPosXTo)
        {
            featherEnabled = true;
        } else
        {
            featherEnabled = false;
        }

        Debug.Log("Position: " + rigidBody.position.x
            + ", Velocity: " + rigidBody.velocity.x
            + ", Acceleration: " + forceX / rigidBody.mass);

        //Feather
        if (featherEnabled)
        {
            //F = -x*k
            forceX = -compression * springConstant;


            //Apply Force
            rigidBody.AddForce(new Vector3(forceX, 0f, 0f));
        }

        //Post-Feather

        currentTimeStep += Time.deltaTime;
        timeSeries.Add(new List<float>() {currentTimeStep, rigidBody.position.x, rigidBody.velocity.x, forceX });
    }

    void OnApplicationQuit() {
        WriteTimeSeriesToCSV();
    }

    void WriteTimeSeriesToCSV() {
        using (var streamWriter = new StreamWriter("time_series.csv")) {
            streamWriter.WriteLine("t,z(t),v(t),a(t) (added)");
            
            foreach (List<float> timeStep in timeSeries) {
                streamWriter.WriteLine(string.Join(",", timeStep));
                streamWriter.Flush();
            }
        }
    }
}
