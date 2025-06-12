using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public PuzzleManager puzzleManager;
    public PuzzleAgent puzzleAgent;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            Debug.Log("Box is inside goal trigger!");

            Transform boxTransform = other.transform;
            Transform goalTransform = transform;

            //Snap box onto goal
            boxTransform.position = new Vector3(goalTransform.position.x, boxTransform.position.y, goalTransform.position.z);

            //Zero box velocity
            Rigidbody boxRB = boxTransform.GetComponent<Rigidbody>();
            boxRB.velocity = Vector3.zero;
            boxRB.angularVelocity = Vector3.zero;

            //Hide them
            boxTransform.gameObject.SetActive(false);
            goalTransform.gameObject.SetActive(false);

            //Give reward
            puzzleAgent.AddReward(1.0f);
            puzzleAgent.EndEpisode();
        }
    }

    //Currently doesn't run but useage would be good for further work
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            puzzleManager.BoxExitedGoal(other.gameObject);
        }
    }
}
