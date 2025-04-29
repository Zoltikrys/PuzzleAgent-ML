using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PuzzleAgent : Agent
{
    public PuzzleManager manager;

    public List<Transform> boxTransforms = new List<Transform>();
    public List<Transform> goalTransforms = new List<Transform>();
    public float moveSpeed = 5f;
    public float pushForce = 5f;

    [SerializeField] private Rigidbody agentRB;

    private List<float> lastBoxToGoalDistances = new List<float>();
    private bool[] boxCompleted;

    public override void Initialize()
    {
        agentRB = GetComponent<Rigidbody>();
        boxCompleted = new bool[boxTransforms.Count];
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);

        for (int i = 0; i < boxTransforms.Count; i++)
        {
            Vector3 boxPos = boxTransforms[i].position;
            Vector3 goalPos = goalTransforms[i].position;

            sensor.AddObservation(boxPos);
            sensor.AddObservation(goalPos);
            sensor.AddObservation(boxPos - transform.position);
            sensor.AddObservation(goalPos - boxPos);
            sensor.AddObservation(transform.position - goalPos);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];
        Vector3 movement = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        agentRB.MovePosition(transform.position + movement);

        AddReward(-0.001f);

        int completedCount = 0;

        for (int i = 0; i < boxTransforms.Count; i++)
        {
            Transform box = boxTransforms[i];
            Transform goal = goalTransforms[i];

            float distToAgent = Vector3.Distance(transform.position, box.position);
            float distToGoal = Vector3.Distance(box.position, goal.position);

            // Small reward for approaching box
            if (distToAgent < 0.5f)
                AddReward(0.001f);

            // Reward for getting closer to goal
            if (distToGoal < lastBoxToGoalDistances[i])
                AddReward(0.002f);

            lastBoxToGoalDistances[i] = distToGoal;

            // If close enough to goal
            if (!boxCompleted[i] && distToGoal < 1.0f)
            {
                boxCompleted[i] = true;
                AddReward(1.0f);
            }

            if (boxCompleted[i]) completedCount++;

            // Push box if nearby
            if (distToAgent < 2f)
            {
                Rigidbody boxRB = box.GetComponent<Rigidbody>();
                Vector3 pushDir = (box.position - transform.position).normalized;
                boxRB.AddForce(pushDir * pushForce, ForceMode.Impulse);
            }

            // Check for stuck box
            if (manager.IsBoxStuck(box))
            {
                Debug.Log("Box stuck: " + i);
                OnFailure();
                return;
            }
        }

        // If all boxes are completed
        if (completedCount == boxTransforms.Count)
        {
            OnBoxInGoal();
        }
    }

    public override void OnEpisodeBegin()
    {
        //transform.localPosition = new Vector3(Random.Range(-5f, 2f), 7.8f, Random.Range(-2f, -8f));
        agentRB.velocity = Vector3.zero;

        //lastBoxToGoalDistances.Clear();
        boxCompleted = new bool[boxTransforms.Count];

        /*for (int i = 0; i < boxTransforms.Count; i++)
        {
            // Reset boxes
            Transform box = boxTransforms[i];
            box.localPosition = new Vector3(Random.Range(-4f, -2f), 7.5f, Random.Range(-7.5f, -2.5f));
            box.GetComponent<Rigidbody>().velocity = Vector3.zero;

            // Reset goals
            Transform goal = goalTransforms[i];
            goal.localPosition = new Vector3(Random.Range(-5f, 2f), 6.77f, Random.Range(-8.6f, -1.6f));

            lastBoxToGoalDistances.Add(Vector3.Distance(box.position, goal.position));
        }
        */
        manager.Reset();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    public void OnBoxInGoal()
    {
        SetReward(2.0f); // Increase total reward if all boxes complete
        EndEpisode();
    }

    public void OnFailure()
    {
        SetReward(-1.0f);
        EndEpisode();
    }
}
