using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;

public class PuzzleAgent : Agent
{
    public PuzzleManager manager;

    public Transform boxTransform; //Reference to box transform
    public Transform goalTransform; //Reference to goal transform
    public float moveSpeed = 5f; //Movement speed of the agent
    public float pushForce = 5f; //Force applied to the box when pushed by the agent

    [SerializeField] private Rigidbody agentRB;

    [SerializeField] private float lastBoxToGoalDistance;

    //Called when the agent is reset/placed into the environment
    public override void Initialize()
    {
        agentRB = GetComponent<Rigidbody>();
    }

    //Collect observations for the agent
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 agentPos = transform.position; // / 10f;
        Vector3 boxPos = boxTransform.position; // / 10f;
        Vector3 goalPos = goalTransform.position; // / 10f;

        //Absolute positions (normalized)
        sensor.AddObservation(agentPos); //Observation 1
        sensor.AddObservation(boxPos); //Observation 2
        sensor.AddObservation(goalPos); //Observation 3

        //Relative positions (normalized)
        sensor.AddObservation(boxPos - agentPos); //Observation 4
        sensor.AddObservation(goalPos - boxPos); //Observation 5
        sensor.AddObservation(agentPos - goalPos); //Observation 6
    }


    //Take actions
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //Actions
        float moveX = actionBuffers.ContinuousActions[0]; //Left/Right
        float moveZ = actionBuffers.ContinuousActions[1]; //Up/Down movement

        // Movement
        Vector3 movement = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        agentRB.MovePosition(transform.position + movement);

        //Visual feedback for movement
        Debug.DrawRay(transform.position, movement, Color.green, 0.5f);

        //Rewards
        float distToGoal = Vector3.Distance(boxTransform.position, goalTransform.position);
        float distToAgent = Vector3.Distance(transform.position, boxTransform.position);

        //Negative reward after each step to encourage faster solution
        AddReward(-0.001f);

        //Small reward for getting close to the box
        if (distToAgent < 0.05f)
            AddReward(0.001f);

        //Small reward for getting box closer to goal
        float previousDistanceToGoal = lastBoxToGoalDistance;
        if (distToGoal < previousDistanceToGoal)
            AddReward(0.002f);

        lastBoxToGoalDistance = distToGoal;

        //Visual feedback for box-to-goal distance
        Debug.DrawLine(boxTransform.position, goalTransform.position, Color.red);

        //Check if the box is stuck
        if (manager.IsBoxStuck(boxTransform))
        {
            Debug.Log("The box is stuck!");
            OnFailure(); //Handle failure if the box is stuck
        }

        // Reward the agent when the box is in the goal
        if (Vector3.Distance(boxTransform.position, goalTransform.position) < 1f)
        {
            manager.BoxInGoal(); //Notify the PuzzleManager
            //OnBoxInGoal();
        }
    }



    //Had problems implementic heuristic method so it's commented out for now

    
    //Called when the agent interacts with the environment
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Log detected inputs
        Debug.Log("Horizontal Input: " + Input.GetAxis("Horizontal"));
        Debug.Log("Vertical Input: " + Input.GetAxis("Vertical"));

        //Set continuous actions
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal"); //Set horizontal movement
        continuousActionsOut[1] = Input.GetAxis("Vertical"); //Set vertical movement
    }
    


    //Reward system
    public override void OnEpisodeBegin()
    {
        //Reset the agent's position, box and goal
        transform.localPosition = new Vector3(Random.Range(-5f, 1.87f), 7.8f, Random.Range(-1.67f, -8.67f));
        boxTransform.localPosition = new Vector3(Random.Range(-4.1f, -2.5f), 7.5f, Random.Range(-7.6f, -2.6f));
        goalTransform.localPosition = new Vector3(Random.Range(-5.12f, 1.9f), 6.77f, Random.Range(-8.65f, -1.6f));

        //Reset velocities
        agentRB.velocity = Vector3.zero;

        Rigidbody boxRB = boxTransform.GetComponent<Rigidbody>();
        boxRB.velocity = Vector3.zero;

        //Reset distance tracker
        lastBoxToGoalDistance = Vector3.Distance(boxTransform.position, goalTransform.position);

        //Reset puzzle state
        manager.Reset();
    }

    /*
    public override void OnEpisodeBegin()
    {
        gameObject.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        gameObject.transform.Rotate(new Vector3(1, 0, 0), Random.Range(-10f, 10f));
        gameObject.transform.Rotate(new Vector3(0, 0, 1), Random.Range(-10f, 10f));
        m_BallRb.velocity = new Vector3(0f, 0f, 0f);
        ball.transform.position = new Vector3(Random.Range(-1.5f, 1.5f), 4f, Random.Range(-1.5f, 1.5f))
            + gameObject.transform.position;
        //Reset the parameters when the Agent is reset.
        SetResetParameters();
    }
    */



    //Reward on completion
    public void OnBoxInGoal()
    {
        SetReward(1.0f);
        EndEpisode();
    }

    //Handle failure
    public void OnFailure()
    {
        SetReward(-1.0f);
        EndEpisode();
    }

    private void Update()
    {
        // Log detected inputs
        Debug.Log("Horizontal Input: " + Input.GetAxis("Horizontal"));
        Debug.Log("Vertical Input: " + Input.GetAxis("Vertical"));
    }
}
