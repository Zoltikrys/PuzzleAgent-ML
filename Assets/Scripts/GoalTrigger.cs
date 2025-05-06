using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public PuzzleManager puzzleManager;
    public PuzzleAgent puzzleAgent;
    //public GameObject goal;

    /*public void Start()
    {
        puzzleAgent = GetComponent<PuzzleAgent>();
    }*/

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            Debug.Log("Box is inside goal trigger!");

            Transform boxTransform = other.transform;
            Transform goalTransform = transform;

            // Snap box onto goal
            boxTransform.position = new Vector3(goalTransform.position.x, boxTransform.position.y, goalTransform.position.z);

            // Zero box velocity
            Rigidbody boxRB = boxTransform.GetComponent<Rigidbody>();
            boxRB.velocity = Vector3.zero;
            boxRB.angularVelocity = Vector3.zero;

            // "Disappear" them
            boxTransform.gameObject.SetActive(false);
            goalTransform.gameObject.SetActive(false);

            // Give reward
            puzzleAgent.AddReward(1.0f);
            puzzleAgent.EndEpisode();

            // You can comment this out for now to isolate testing
            // puzzleAgent.matchedPairs.Add((boxTransform, goalTransform));
            // if (puzzleAgent.matchedPairs.Count == Mathf.Min(puzzleAgent.boxTransforms.Count, puzzleAgent.goalTransforms.Count))
            // {
            //     puzzleAgent.OnPuzzleComplete();
            // }
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            puzzleManager.BoxExitedGoal(other.gameObject);
        }
    }
}
