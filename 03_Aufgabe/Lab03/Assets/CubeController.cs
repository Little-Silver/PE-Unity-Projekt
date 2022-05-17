using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using System.IO;

/*
    Accelerates the cube to which it is attached, modelling an harmonic oscillator.
    Writes the position, velocity and acceleration of the cube to a CSV file.
    
    Remark: For use in "Physics Engines" module at ZHAW, part of physics lab
    Author: kemf
    Version: 1.0
*/
public class CubeController : MonoBehaviour
{
    private const int DELAY = 4;

    public float springLength; // m
    public float springConstant; // N/m
    public float startVelocity; // m/s
    private float velocity1;
    private float velocity2;
    public Rigidbody lightCube;
    private Rigidbody heavyCube;
    private float previousDistance = 10.0f;
    private bool isPushed = false;
    private float currentTimeStep; // s
    private float pushTime; // s
    private bool catchCube;
    private float radius = 5.0f;
    float slowdown;
    private List<List<float>> timeSeries;

    private bool turning = false;

    // Start is called before the first frame update
    void Start()
    {
        heavyCube = GetComponent<Rigidbody>();
        timeSeries = new List<List<float>>();
        velocity1 = startVelocity;
        velocity2 = 0;
        slowdown = 0.0f;
        lightCube.velocity = new Vector3(velocity1, 0, 0);
        heavyCube.velocity = new Vector3(velocity2, 0, 0);
        catchCube = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    // FixedUpdate can be called multiple times per frame
    void FixedUpdate()
    {
        currentTimeStep += Time.deltaTime;
        timeSeries.Add(new List<float>() { currentTimeStep, lightCube.velocity.x, heavyCube.velocity.x, heavyCube.position.x, heavyCube.position.y, heavyCube.position.z });

        float distance = heavyCube.position.x - 0.5f - (lightCube.position.x + 0.5f);
        float forceX = springConstant * (springLength - distance);

        if (distance <= springLength && !isPushed)
        {
            lightCube.AddForce(new Vector3(-forceX, 0, 0));
            heavyCube.AddForce(new Vector3(forceX, 0, 0));

            if (distance <= previousDistance)
            {
                previousDistance = distance;
            }
            else
            {
                isPushed = true;
                pushTime = currentTimeStep;
            }
        }
        else if (isPushed)
        {
            if (currentTimeStep - pushTime < DELAY)
            {
                lightCube.AddForce(new Vector3(0, 0, 0));
                heavyCube.AddForce(new Vector3(0, 0, 0));
                lightCube.velocity = heavyCube.velocity;
            }
            else if (distance <= springLength)
            {
                lightCube.AddForce(new Vector3(-forceX, 0, 0));
                heavyCube.AddForce(new Vector3(forceX, 0, 0));
                catchCube = true;
            }
        }
        if (catchCube)
        {
            quarterCircle();
        }
    }

    void quarterCircle()
    {
        if (lightCube.position.x <= 5 && !turning)
        {
            turning = true;
            //the value has to be constant so it is created here once.
            slowdown = lightCube.mass * lightCube.velocity.sqrMagnitude / (radius * (float)Math.PI);  //v^2/2*s => m*v^2/r*pi
        }
        if (turning)
        {
            if (lightCube.velocity.sqrMagnitude < 0.001f)
            {
                lightCube.velocity = Vector3.zero;
                turning = false;
            }

            lightCube.transform.rotation = Quaternion.LookRotation(lightCube.velocity, Vector3.up);
            Vector3 F_slowdown = -lightCube.transform.forward * slowdown;

            float forceC = lightCube.velocity.sqrMagnitude / radius * lightCube.mass; //  centripetal force is calculated as follow: v^2/r * m
            Vector3 F_centripetal = Vector3.Cross(lightCube.velocity.normalized, Vector3.up * forceC); // creates the Vector we need

            Vector3 F_res = F_centripetal + F_slowdown;
            lightCube.AddForce(F_res);
        }
    }

    void OnApplicationQuit()
    {
        WriteTimeSeriesToCSV();
    }

    void WriteTimeSeriesToCSV()
    {
        using (var streamWriter = new StreamWriter("time_series.csv"))
        {
            streamWriter.WriteLine("t, v_lightCube,v_heavyCube, x(t)_heavyCube, y(t)_heavyCube, z(t)_heavyCube");

            foreach (List<float> timeStep in timeSeries)
            {
                streamWriter.WriteLine(string.Join(",", timeStep));
                streamWriter.Flush();
            }
        }
    }
}
