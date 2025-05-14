using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;
using Unity.VisualScripting;

public class PuzzleAgent : Agent
{
    public PuzzleManager manager;

    private float lastTotalDistance;

    private List<(Transform box, Transform goal)> matchedPairs = new List<(Transform, Transform)>();
    
    // Track the minimum distance between each box and goal during the episode
    private Dictionary<Transform, float> minBoxGoalDistances = new Dictionary<Transform, float>();


    public List<Transform> boxTransforms; // References to multiple box transforms
    public List<Transform> goalTransforms; // References to multiple goal transforms

    public float moveSpeed = 5f; // Movement speed of the agent
    public float pushForce = 5f; // Force applied to the box when pushed by the agent

    [SerializeField] private Rigidbody agentRB;

    // Called when the agent is reset/placed into the environment
    public override void Initialize()
    {
        agentRB = GetComponent<Rigidbody>();
    }

    // Collect observations for the agent
    [SerializeField] private int maxBoxes = 4;
    [SerializeField] private int maxGoals = 4;

    private Dictionary<Transform, float> lastBoxDistances = new Dictionary<Transform, float>();

    private List<(Transform, Transform)> GetUnmatchedPairs()
    {
        var unmatched = new List<(Transform, Transform)>();
        foreach (Transform box in boxTransforms)
        {
            if (matchedPairs.Exists(p => p.box == box)) continue;
            Transform nearestGoal = null;
            float minDist = float.MaxValue;
            foreach (Transform goal in goalTransforms)
            {
                if (matchedPairs.Exists(p => p.goal == goal)) continue;
                float dist = Vector3.Distance(box.position, goal.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestGoal = goal;
                }
            }
            if (nearestGoal != null)
                unmatched.Add((box, nearestGoal));
        }
        return unmatched;
    }



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

    // Take actions
    // Replace the entire OnActionReceived method with this:

    public override void OnActionReceived(ActionBuffers actionBuffers)
{
    // === Move agent ===
    float moveX = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
    float moveZ = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
    Vector3 movement = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
    agentRB.MovePosition(transform.position + movement);

    // === Step penalty to encourage efficiency ===
    AddReward(-0.001f);

    // === Closest box logic ===
    Transform closestBox = null;
    float closestDist = float.MaxValue;

    foreach (Transform box in boxTransforms)
    {
        if (matchedPairs.Exists(pair => pair.box == box)) continue;

        float dist = Vector3.Distance(transform.position, box.position);
        if (dist < closestDist)
        {
            closestDist = dist;
            closestBox = box;
        }
    }

    // Reward for getting closer to the box
    if (closestBox != null)
    {
        float distToBox = Vector3.Distance(transform.position, closestBox.position);
        float boxApproachReward = Mathf.Clamp01(1f - distToBox / 10f); // reward for getting closer to the box
        AddReward(boxApproachReward * 0.01f);
    }

    // === Reward for pushing boxes in the correct direction ===
    foreach ((Transform box, Transform goal) in GetUnmatchedPairs())
    {
        Rigidbody boxRB = box.GetComponent<Rigidbody>();
        float distToGoal = Vector3.Distance(box.position, goal.position);

        // Initialize or update the minimum distance for this box-goal pair
        if (!minBoxGoalDistances.ContainsKey(box) || distToGoal < minBoxGoalDistances[box])
        {
            minBoxGoalDistances[box] = distToGoal;
        }

        // Reward for making progress by getting closer to the goal
        if (distToGoal < minBoxGoalDistances[box])
        {
            float distProgress = minBoxGoalDistances[box] - distToGoal;
            AddReward(distProgress * 0.01f); // reward for moving closer
            minBoxGoalDistances[box] = distToGoal; // update minimum distance
        }

        // Penalize for moving the box away from the goal
        else if (distToGoal > minBoxGoalDistances[box])
        {
            AddReward(-0.05f); // penalize for moving away from the goal
        }

        // Non-linear proximity reward (higher reward when closer to goal)
        float proximityReward = Mathf.Pow(Mathf.Clamp01(1f - distToGoal / 10f), 2f);
        AddReward(proximityReward * 0.1f);
    }

    // === Matching logic ===
    for (int i = 0; i < boxTransforms.Count; i++)
    {
        Transform box = boxTransforms[i];
        if (matchedPairs.Exists(pair => pair.box == box)) continue;

        for (int j = 0; j < goalTransforms.Count; j++)
        {
            Transform goal = goalTransforms[j];
            if (matchedPairs.Exists(pair => pair.goal == goal)) continue;

            Vector2 boxPos2D = new Vector2(box.position.x, box.position.z);
            Vector2 goalPos2D = new Vector2(goal.position.x, goal.position.z);

            if (Vector2.Distance(boxPos2D, goalPos2D) < 0.25f)
            {
                box.position = new Vector3(goal.position.x, box.position.y, goal.position.z);
                box.GetComponent<Rigidbody>().velocity = Vector3.zero;
                box.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

                matchedPairs.Add((box, goal));
                box.gameObject.SetActive(false);
                goal.gameObject.SetActive(false);

                AddReward(1.0f); // success reward
                break;
            }
        }
    }

    // === Completion check ===
    if (matchedPairs.Count == Mathf.Min(boxTransforms.Count, goalTransforms.Count))
    {
        AddReward(2.0f); // big bonus
        EndEpisode();
    }
}








    // Reward on completion
    public void OnPuzzleComplete()
    {
        AddReward(2.0f); // Big bonus reward
        EndEpisode();
    }

    // Called when all boxes are in goals
    public void OnBoxInGoal()
    {
        SetReward(1.0f);
        /////////////////////////////////////////////////////////////EndEpisode(); THIS LINE WASTED SO MUCH TIME
    }

    // Reward system
    public void OnFailure()
    {
        SetReward(-1.0f);
        EndEpisode();
    }

    private void Update()
    {

        //CheckIfFallen();

        /*
        // Log detected inputs
        Debug.Log("Horizontal Input: " + Input.GetAxis("Horizontal"));
        Debug.Log("Vertical Input: " + Input.GetAxis("Vertical"));
        */
    }

    // Called when the agent interacts with the environment
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        /*
        // Log detected inputs
        Debug.Log("Horizontal Input: " + Input.GetAxis("Horizontal"));
        Debug.Log("Vertical Input: " + Input.GetAxis("Vertical"));
        */

        // Set continuous actions
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal"); // Set horizontal movement
        continuousActionsOut[1] = Input.GetAxis("Vertical"); // Set vertical movement
    }

    // Called when the agent is reset
    public override void OnEpisodeBegin()
    {
        // Reactivate all boxes and goals
        foreach (Transform box in boxTransforms)
        {
            box.gameObject.SetActive(true);
        }

        foreach (Transform goal in goalTransforms)
        {
            goal.gameObject.SetActive(true);
        }

        // Clear matched pairs
        matchedPairs.Clear();


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


        /*
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

        */


        // Reset puzzle state
        manager.Reset();
    }

    // Gizmos for debugging visualization
    private void OnDrawGizmos()
    {
        // Draw lines from agent to selected box (assuming the agent is selecting the first box)
        if (boxTransforms.Count > 0)
        {
            Gizmos.color = Color.red; // Set color for agent to box line
            Gizmos.DrawLine(transform.position, boxTransforms[0].position); // Line from agent to the first box
        }

        // Draw lines from each box to each goal
        for (int i = 0; i < boxTransforms.Count; i++)
        {
            for (int j = 0; j < goalTransforms.Count; j++)
            {
                Gizmos.color = Color.blue; // Set color for box to goal lines
                Gizmos.DrawLine(boxTransforms[i].position, goalTransforms[j].position); // Line from box to each goal
            }
        }
    }

    private void CheckIfFallen() //added because in training the agent kept falling off the map
    {
        if (transform.localPosition.y < 5f) // adjust threshold depending on your map
        {
            OnFailure();
        }
    }

}
