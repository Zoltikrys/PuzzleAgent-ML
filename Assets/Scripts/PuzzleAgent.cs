using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;

public class PuzzleAgent : Agent
{
    public PuzzleManager manager;

    public Transform[] boxTransforms;
    public Transform[] goalTransforms;
    public float moveSpeed = 5f;
    public float pushForce = 5f;

    [SerializeField] private Rigidbody agentRB;

    private HashSet<Transform> matchedBoxes = new HashSet<Transform>();
    private HashSet<Transform> matchedGoals = new HashSet<Transform>();
    private const float goalThreshold = 1.0f;

    private float[] lastBoxGoalDistances;

    public override void Initialize()
    {
        agentRB = GetComponent<Rigidbody>();
        lastBoxGoalDistances = new float[boxTransforms.Length];
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 agentPos = transform.position;
        sensor.AddObservation(agentPos);

        foreach (var box in boxTransforms)
        {
            sensor.AddObservation(box.position);
            sensor.AddObservation(box.position - agentPos);
        }

        foreach (var goal in goalTransforms)
        {
            sensor.AddObservation(goal.position);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];

        Vector3 movement = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        agentRB.MovePosition(transform.position + movement);

        AddReward(-0.001f);

        for (int i = 0; i < boxTransforms.Length; i++)
        {
            var box = boxTransforms[i];
            float distToAgent = Vector3.Distance(transform.position, box.position);
            float distToGoal = GetClosestGoalDistance(box);

            if (distToAgent < 0.05f)
                AddReward(0.001f);

            if (distToGoal < lastBoxGoalDistances[i])
                AddReward(0.002f);

            lastBoxGoalDistances[i] = distToGoal;

            if (manager.IsBoxStuck(box))
            {
                Debug.Log("A box is stuck!");
                OnFailure();
                return;
            }

            if (distToAgent < 2f)
            {
                Vector3 pushDir = (box.position - transform.position).normalized;
                Rigidbody boxRB = box.GetComponent<Rigidbody>();
                boxRB.AddForce(pushDir * pushForce, ForceMode.Impulse);
            }
        }

        CheckForGoals();
    }

    private float GetClosestGoalDistance(Transform box)
    {
        float closest = float.MaxValue;
        foreach (var goal in goalTransforms)
        {
            float dist = Vector3.Distance(box.position, goal.position);
            if (dist < closest)
                closest = dist;
        }
        return closest;
    }

    private void CheckForGoals()
    {
        foreach (var box in boxTransforms)
        {
            if (matchedBoxes.Contains(box)) continue;

            foreach (var goal in goalTransforms)
            {
                if (matchedGoals.Contains(goal)) continue;

                if (Vector3.Distance(box.position, goal.position) < goalThreshold)
                {
                    matchedBoxes.Add(box);
                    matchedGoals.Add(goal);

                    AddReward(1.0f);
                    Debug.Log("Box placed in goal!");

                    if (matchedBoxes.Count >= goalTransforms.Length)
                    {
                        EndEpisode();
                    }

                    return;
                }
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        matchedBoxes.Clear();
        matchedGoals.Clear();

        transform.localPosition = new Vector3(Random.Range(-5f, 1.87f), 7.8f, Random.Range(-1.67f, -8.67f));
        agentRB.velocity = Vector3.zero;

        for (int i = 0; i < boxTransforms.Length; i++)
        {
            var box = boxTransforms[i];
            box.localPosition = new Vector3(Random.Range(-4.1f, -2.5f), 7.5f, Random.Range(-7.6f, -2.6f));
            box.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }

        for (int i = 0; i < goalTransforms.Length; i++)
        {
            goalTransforms[i].localPosition = new Vector3(Random.Range(-5.12f, 1.9f), 6.77f, Random.Range(-8.65f, -1.6f));
        }

        lastBoxGoalDistances = new float[boxTransforms.Length];

        for (int i = 0; i < boxTransforms.Length; i++)
        {
            lastBoxGoalDistances[i] = GetClosestGoalDistance(boxTransforms[i]);
        }

        manager.Reset();
    }

    public void OnBoxInGoal()
    {
        SetReward(1.0f);
        EndEpisode();
    }

    public void OnFailure()
    {
        SetReward(-1.0f);
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}
