using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [SerializeField] private bool puzzleComplete = false;

    public void BoxInGoal()
    {
        if (!puzzleComplete)
        {
            puzzleComplete = true;
            Debug.Log("Puzzle complete!");
            //Reward agent
        }
    }

    public void Reset()
    {
        //Reset logic here


        puzzleComplete = false;
    }




    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
