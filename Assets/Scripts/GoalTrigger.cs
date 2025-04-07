using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public PuzzleManager puzzleManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            puzzleManager.BoxInGoal();
        }
    }
}
