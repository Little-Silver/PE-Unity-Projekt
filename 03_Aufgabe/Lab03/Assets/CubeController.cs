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

    public float springLength;   // m
    public float springConstant; // N/m
    public float startVelocity;  // m/s
    private float velocity1;     // m/s
    private float velocity2;     // m/s
    public Rigidbody lightCube;
    private Rigidbody heavyCube;
    private float previousDistance = 10.0f; // m
    private float currentTimeStep; // s
    private float pushTime;        // s
    private float radius = 5.0f;
    float slowdown;
    private List<List<float>> timeSeries;

    private State state;

    private bool isRotating = false;


    // Start is called before the first frame update
    void Start()
    {
        heavyCube = GetComponent<Rigidbody>();
        timeSeries = new List<List<float>>();
        velocity1 = startVelocity; // m/s
        velocity2 = 0; // m/s
        slowdown = 0.0f;
        lightCube.velocity = new Vector3(velocity1, 0, 0);
        heavyCube.velocity = new Vector3(velocity2, 0, 0);
        state = State.PRE_SPRING;

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

        float distanceBetweenCubes = heavyCube.position.x - 0.5f - (lightCube.position.x + 0.5f);
        float forceX = springConstant * (springLength - distanceBetweenCubes);

        switch (state)
        {
            case State.PRE_SPRING:
                if (cubesAreTouchingSpring(distanceBetweenCubes, springLength))
                {
                    state = State.TOUCHING_BEFORE_LOCK;
                    goto case State.TOUCHING_BEFORE_LOCK;
                }
                break;
            case State.TOUCHING_BEFORE_LOCK:
                applyForce(-forceX, forceX);

                if (maxSpringCompression(distanceBetweenCubes, previousDistance))
                {
                    state = State.CONNECTED;
                    pushTime = currentTimeStep;
                }
                else
                {
                    previousDistance = distanceBetweenCubes;
                }
                break;
            case State.CONNECTED:
                if (currentTimeStep - pushTime >= DELAY)
                {
                    state = State.TOUCHING_AFTER_LOCK;
                    goto case State.TOUCHING_AFTER_LOCK;
                } 
                applyForce(0, 0);
                lightCube.velocity = heavyCube.velocity;
                break;
            case State.TOUCHING_AFTER_LOCK:
                if (distanceBetweenCubes > springLength)
                {
                    state = State.POST_SPRING;
                    goto case State.POST_SPRING;
                } 
                applyForce(-forceX, forceX);
                break;
            case State.POST_SPRING:
                quarterCircle();
                break;
            default:
                break;
        }

        Debug.Log("State = " + state 
                + ", Heavy-Cube Velocity (X, Y, Z): " + heavyCube.velocity.x + ", " + heavyCube.velocity.y + ", " + heavyCube.velocity.z
                + ", Heavy-Cube Position (X, Y, Z): " + heavyCube.position.x + ", " + heavyCube.position.y + ", " + heavyCube.position.z);
        Debug.Log("State = " + state
                + ", Light-Cube Velocity (X, Y, Z): " + lightCube.velocity.x + ", " + lightCube.velocity.y + ", " + lightCube.velocity.z
                + ", Light-Cube Position (X, Y, Z): " + lightCube.position.x + ", " + lightCube.position.y + ", " + lightCube.position.z);

    }

    void quarterCircle()
    {
        if (lightCube.position.x <= 5 && !isRotating)
        {
            isRotating = true;
            //the value has to be constant so it is created here once.
            slowdown = lightCube.mass * lightCube.velocity.sqrMagnitude / (radius * (float)Math.PI);  // v^2/2*s => m*v^2/r*pi
        }
        if (isRotating)
        {
            if (lightCube.velocity.sqrMagnitude < 0.001f)
            {
                lightCube.velocity = Vector3.zero;
                isRotating = false;
            }

            lightCube.transform.rotation = Quaternion.LookRotation(lightCube.velocity, Vector3.up);
            Vector3 F_slowdown = -lightCube.transform.forward * slowdown;

            float forceC = lightCube.velocity.sqrMagnitude / radius * lightCube.mass; //  centripetal force is calculated as follow: v^2/r * m
            Vector3 F_centripetal = Vector3.Cross(lightCube.velocity.normalized, Vector3.up * forceC); // creates the Vector we need

            Vector3 F_res = F_centripetal + F_slowdown;
            lightCube.AddForce(F_res);
        }
    }

    bool cubesAreTouchingSpring(float distanceBetweenCubes, float springLength)
    {
        return (distanceBetweenCubes <= springLength);
    }

    void applyForce(float lightCubeX, float heavyCubeX)
    {
        lightCube.AddForce(new Vector3(lightCubeX, 0, 0));
        heavyCube.AddForce(new Vector3(heavyCubeX, 0, 0));
    }

    bool maxSpringCompression(float distanceBetweenCubes, float previousDistance)
    {
        return (distanceBetweenCubes > previousDistance);
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

enum State
{
    ///<summary>This state is applied until the light (left) cube touches the spring the first time.</summary>
    PRE_SPRING,
    ///<summary>This state is used while the spring is still being compressed. </summary>
    TOUCHING_BEFORE_LOCK,
    ///<summary>This state is used while the spring is staying at max compression for a certain time period. </summary>
    CONNECTED,
    ///<summary>This state is used once the spring is "unlocked" but the cubes are still touching the spring. </summary>
    TOUCHING_AFTER_LOCK,
    ///<summary>This state is used once the spring spring doesn't touch the cubes anymore. </summary>
    POST_SPRING
}    
