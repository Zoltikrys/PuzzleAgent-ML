using System.Collections.Generic;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [SerializeField] private PuzzleAgent agent;

    [SerializeField] private int totalBoxesNeeded = 3; // Set this in Inspector
    private HashSet<GameObject> boxesInGoals = new HashSet<GameObject>();

    private bool puzzleComplete = false;

    public void BoxEnteredGoal(GameObject box)
    {
        if (!boxesInGoals.Contains(box))
        {
            boxesInGoals.Add(box);

            // Reward the agent each time a new box reaches a goal
            agent.OnBoxInGoal();

            Debug.Log($"Boxes in goals: {boxesInGoals.Count}/{totalBoxesNeeded}");

            // Check for puzzle completion
            if (boxesInGoals.Count >= totalBoxesNeeded && !puzzleComplete)
            {
                puzzleComplete = true;
                Debug.Log("Puzzle complete! All boxes in goals.");
                // Optionally reward extra for full completion
                agent.OnPuzzleComplete();
            }
        }
    }

    public void BoxExitedGoal(GameObject box)
    {
        if (boxesInGoals.Contains(box))
        {
            boxesInGoals.Remove(box);
            Debug.Log($"Box exited goal. Boxes in goals: {boxesInGoals.Count}/{totalBoxesNeeded}");
        }
    }

    public void Reset()
    {
        puzzleComplete = false;
        boxesInGoals.Clear();
    }

    public bool IsBoxStuck(Transform box)
    {
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        foreach (Vector3 dir in directions)
        {
            Vector3 origin = box.position;
            Debug.DrawRay(box.position, dir, Color.yellow, 0.5f);
            if (Physics.Raycast(origin, dir, out RaycastHit hit, 1f))
            {
                if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Box"))
                {
                    continue;
                }

                if (hit.collider.CompareTag("Floor"))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
