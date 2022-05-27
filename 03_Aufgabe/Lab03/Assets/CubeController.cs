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
    private float RADIUS = 5.0f;
    private float GRAVITY = 9.81f;
    private float mu;
    private List<List<float>> timeSeries;
    private Vector3 vectorInit = new Vector3(5f, 0.5f, -4f);
    private State state;

    private bool isRotating = false;


    // Start is called before the first frame update
    void Start()
    {
        heavyCube = GetComponent<Rigidbody>();
        timeSeries = new List<List<float>>();
        velocity1 = startVelocity; // m/s
        velocity2 = 0; // m/s
        lightCube.velocity = new Vector3(velocity1, 0, 0);
        heavyCube.velocity = new Vector3(velocity2, 0, 0);
        state = State.PRE_SPRING;
        vectorInit = new Vector3(5f, 0.5f, -4f);
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
                rotate();
                break;
            default:
                break;
        }
    }

    void rotate()
    {
        debugCube(lightCube, state);

        if (lightCube.position.x <= 5 && !isRotating)
        {
            isRotating = true;
            mu = lightCube.velocity.sqrMagnitude / (RADIUS * (float) Math.PI * GRAVITY); // v^2/2*s => m*v^2/r*pi
        }
        if (isRotating)
        {
            if (hasVirtuallyStopped(lightCube))
            {
                lightCube.velocity = Vector3.zero;
                isRotating = false;
            }

            // Einheitsvektor der Zentripetalkraft
            Vector3 ez = Vector3.Normalize(lightCube.position - vectorInit);

            // Zentripetalkraft: F_{Z} = m * v^2 / R
            float forceZ = lightCube.mass * lightCube.velocity.sqrMagnitude / RADIUS;
            Vector3 forceCentriputal = -ez * forceZ;
            //Debug.Log("F_Z: " + vectorString(forceCentriputal));

            // Reibungskraft: F_{R} = g * m * mu
            Vector3 forceFriction = -Vector3.Normalize(lightCube.velocity) * GRAVITY * lightCube.mass * mu;
            //Debug.Log("F_R: " + vectorString(forceFriction));

            // Resultierende Kraft: F_{Res} = F_{Z} + F_{R}
            Vector3 forceResulting = forceCentriputal + forceFriction;
            lightCube.AddForce(forceResulting);
        }
    }

    bool hasVirtuallyStopped(Rigidbody lightCube)
    {
        return lightCube.velocity.sqrMagnitude < 0.001f;
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

    void debugCube(Rigidbody cube, State state)
    {
        Debug.Log("State = " + state
        + ", Light-Cube Velocity: " + cube.velocity.magnitude + " (X, Y, Z): " + cube.velocity.x + ", " + cube.velocity.y + ", " + cube.velocity.z
        + ", Light-Cube Position (X, Y, Z): " + cube.position.x + ", " + cube.position.y + ", " + cube.position.z);
    }

    String vectorString(Vector3 vector)
    {
        return " " + vector.magnitude + " (X, Y, Z): " + vector.x + ", " + vector.y + ", " + vector.z;
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
