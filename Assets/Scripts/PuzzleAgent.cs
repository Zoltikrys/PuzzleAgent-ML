using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PuzzleAgent : Agent
{
    public Transform boxTransform; //Reference to box transform
    public Transform goalTransform; //Reference to goal transform
    public float moveSpeed = 5f; //Movement peed of the agent
    public float pushForce = 5f; //Force applied to the box when pushed by the agent

    [SerializeField] private Rigidbody agentRB;

    //Called when the agent is reset/placed into the environment
    public override void Initialize()
    {
        agentRB = GetComponent<Rigidbody>();
    }

    //Collect observations for the agent
    public override void CollectObservations(VectorSensor sensor)
    {
        //Add the agent's position
        sensor.AddObservation(transform.position);

        //Add the box's position
        sensor.AddObservation(boxTransform.position);

        //Add the goal's position
        sensor.AddObservation(goalTransform.position);
    }

    //Take actions
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //Actions
        float moveX = actionBuffers.ContinuousActions[0]; //Left/Right
        float moveZ = actionBuffers.ContinuousActions[1]; //Up/Down movement
        bool isPushing = actionBuffers.DiscreteActions[2] > 0;


        //Movement
        Vector3 movement = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        agentRB.MovePosition(transform.position + movement);

        //Pushing
        if (isPushing && Vector3.Distance(transform.position, boxTransform.position) <2f)
        {
            //Apply force to the box
            Vector3 pushDirection = (boxTransform.position - transform.position).normalized;
            Rigidbody boxRB = boxTransform.GetComponent<Rigidbody>();
            boxRB.AddForce(pushDirection * pushForce, ForceMode.Impulse);
        }
    }

    //Called when the agent interacts with the environment
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //Set continuous actions
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal"); //Set horizontal movement
        continuousActionsOut[1] = Input.GetAxis("Vertical"); //Set vertical movement

        //Set discrete actions
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0; //Push with space
    }

    //Reward system
    public override void OnEpisodeBegin()
    {
        //Reset the agent's position, box and goal
        transform.position = new Vector3(1f, 1f, -1f);
        boxTransform.position = new Vector3(2f, 0.5f, -1f);

        //Reset velocities
        Rigidbody boxRB = boxTransform.GetComponent<Rigidbody>();
        boxRB.velocity = Vector3.zero;
    }

    //Reward system
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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
