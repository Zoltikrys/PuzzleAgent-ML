using System.Collections.Generic;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [SerializeField] private PuzzleAgent agent;

    [SerializeField] private int totalBoxesNeeded = 3; //Set this in Inspector
    private HashSet<GameObject> boxesInGoals = new HashSet<GameObject>();
    private bool puzzleComplete = false;
    public float trainingSpeed = 10f;

    public void BoxEnteredGoal(GameObject box)
    {
        if (!boxesInGoals.Contains(box))
        {
            boxesInGoals.Add(box);
            Debug.Log($"Boxes in goals: {boxesInGoals.Count}/{totalBoxesNeeded}");

            //Check for puzzle completion
            if (boxesInGoals.Count >= totalBoxesNeeded && !puzzleComplete)
            {
                puzzleComplete = true;
                Debug.Log("Puzzle complete! All boxes in goals.");
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
}
