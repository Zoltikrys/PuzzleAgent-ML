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

    private float lastTotalDistance;


    public List<Transform> boxTransforms; // References to multiple box transforms
    public List<Transform> goalTransforms; // References to multiple goal transforms

    public float moveSpeed = 5f; //Movement speed of the agent
    public float pushForce = 5f; //Force applied to the box when pushed by the agent

    [SerializeField] private Rigidbody agentRB;

    //[SerializeField] private float lastBoxToGoalDistance;

    //Called when the agent is reset/placed into the environment
    public override void Initialize()
    {
        agentRB = GetComponent<Rigidbody>();
    }

    //Collect observations for the agent
    [SerializeField] private int maxBoxes = 4;
    [SerializeField] private int maxGoals = 4;

    public override void CollectObservations(VectorSensor sensor)
    {
        // Observe agent position
        sensor.AddObservation(transform.position);

        // Observe agent velocity
        sensor.AddObservation(agentRB.velocity);

        // === Observe boxes ===
        int boxCount = boxTransforms.Count;

        for (int i = 0; i < maxBoxes; i++)
        {
            if (i < boxCount)
            {
                Transform box = boxTransforms[i];
                sensor.AddObservation(box.position);
                sensor.AddObservation(box.position - transform.position);

                Rigidbody boxRB = box.GetComponent<Rigidbody>();
                sensor.AddObservation(boxRB.velocity);
            }
            else
            {
                // Padding: add zero observations
                sensor.AddObservation(Vector3.zero); // box.position
                sensor.AddObservation(Vector3.zero); // box relative position
                sensor.AddObservation(Vector3.zero); // box velocity
            }
        }

        // === Observe goals ===
        int goalCount = goalTransforms.Count;

        for (int i = 0; i < maxGoals; i++)
        {
            if (i < goalCount)
            {
                Transform goal = goalTransforms[i];
                sensor.AddObservation(goal.position);
                sensor.AddObservation(goal.position - transform.position);
            }
            else
            {
                // Padding: add zero observations
                sensor.AddObservation(Vector3.zero); // goal.position
                sensor.AddObservation(Vector3.zero); // goal relative position
            }
        }
    }





    //Take actions
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];

        // Move agent
        Vector3 movement = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        agentRB.MovePosition(transform.position + movement);

        // Small step penalty to encourage speed
        AddReward(-0.001f);

        // Calculate total distance from each box to its nearest goal
        float totalDistance = 0f;

        for (int i = 0; i < maxBoxes; i++)
        {
            if (i < boxTransforms.Count)
            {
                Transform box = boxTransforms[i];
                float closestGoalDist = float.MaxValue;

                for (int j = 0; j < maxGoals; j++)
                {
                    if (j < goalTransforms.Count)
                    {
                        Transform goal = goalTransforms[j];
                        float dist = Vector3.Distance(box.position, goal.position);
                        if (dist < closestGoalDist)
                            closestGoalDist = dist;
                    }
                }

                totalDistance += closestGoalDist;
            }
            else
            {
                // Padding case — ignore non-existent boxes
            }
        }

        // Check if all boxes are in their goals
        if (AreAllBoxesInGoals())
        {
            OnPuzzleComplete();
        }
    }

    // Method to check if all boxes are in their respective goals
    private bool AreAllBoxesInGoals()
    {
        foreach (Transform box in boxTransforms)
        {
            bool boxInGoal = false;
            foreach (Transform goal in goalTransforms)
            {
                if (Vector3.Distance(box.position, goal.position) < 0.1f) // Adjust threshold as needed
                {
                    boxInGoal = true;
                    break;
                }
            }

            if (!boxInGoal) return false; // If any box is not in a goal, return false
        }

        return true; // If all boxes are in goals, return true
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
        // Reset agent position
        transform.localPosition = new Vector3(Random.Range(-8.3f, 5f), 7.8f, Random.Range(-11.65f, 1.35f));
        agentRB.velocity = Vector3.zero;

        // Reset each box
        foreach (Transform box in boxTransforms)
        {
            box.localPosition = new Vector3(Random.Range(-8.3f, 5f), 7.5f, Random.Range(-11.65f, 1.35f));
            Rigidbody boxRB = box.GetComponent<Rigidbody>();
            boxRB.velocity = Vector3.zero;
        }

        foreach (Transform goal in goalTransforms)
        {
            goal.localPosition = new Vector3(Random.Range(-8.3f, 5f), 6.77f, Random.Range(-11.65f, 1.35f));
        }

        // Reset total distance tracker (matching padding logic)
        float totalDistance = 0f;

        for (int i = 0; i < maxBoxes; i++)
        {
            if (i < boxTransforms.Count)
            {
                Transform box = boxTransforms[i];

                // Find the closest goal for this box
                float closestGoalDist = float.MaxValue;

                foreach (Transform goal in goalTransforms)
                {
                    float dist = Vector3.Distance(box.position, goal.position);
                    if (dist < closestGoalDist)
                        closestGoalDist = dist;
                }

                totalDistance += closestGoalDist;
            }
            else
            {
                // Padding: no box here, so we just add 0 distance
                totalDistance += 0f;
            }
        }

        lastTotalDistance = totalDistance;

        // Reset puzzle state
        manager.Reset();
    }





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

    // Called when all boxes are in goals
    public void OnPuzzleComplete()
    {
        AddReward(2.0f); // Big bonus reward
        EndEpisode();
    }



}
