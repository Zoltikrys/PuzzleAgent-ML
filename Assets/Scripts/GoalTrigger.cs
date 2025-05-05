using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public PuzzleManager puzzleManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            puzzleManager.BoxEnteredGoal(other.gameObject);
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
