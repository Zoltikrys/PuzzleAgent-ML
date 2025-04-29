using UnityEngine;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour
{
    [SerializeField] private PuzzleAgent agent;

    private int totalGoals = 0;
    private HashSet<Transform> matchedGoals = new HashSet<Transform>();
    private bool puzzleComplete = false;

    public void SetGoalCount(int count)
    {
        totalGoals = count;
        matchedGoals.Clear();
        puzzleComplete = false;
    }

    public void BoxInGoal(Transform goal)
    {
        if (puzzleComplete) return;

        if (!matchedGoals.Contains(goal))
        {
            matchedGoals.Add(goal);
            Debug.Log($"Goal matched ({matchedGoals.Count}/{totalGoals})");

            if (matchedGoals.Count >= totalGoals)
            {
                puzzleComplete = true;
                Debug.Log("Puzzle complete!");
                agent.OnBoxInGoal();
            }
        }
    }

    public void Reset()
    {
        matchedGoals.Clear();
        puzzleComplete = false;
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
                    continue;

                if (hit.collider.CompareTag("Floor"))
                    return false;
            }
            else
            {
                return false; // open space
            }
        }

        return true; // all directions blocked
    }
}
