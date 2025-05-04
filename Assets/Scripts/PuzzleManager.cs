using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [SerializeField] private bool puzzleComplete = false;
    [SerializeField] private PuzzleAgent agent;

    public void BoxInGoal()
    {
        if (!puzzleComplete)
        {
            puzzleComplete = true;
            Debug.Log("Puzzle complete!");

            //Reward agent
            agent.OnBoxInGoal();
        }
    }

    
    public void Reset()
    {
        /*
        // Reset agent's position
        agent.transform.position = new Vector3(1f, 1f, -1f);

        // Reset box position and velocity
        agent.boxTransform.position = new Vector3(2f, 0.5f, -1f);
        Rigidbody boxRB = agent.boxTransform.GetComponent<Rigidbody>();
        boxRB.velocity = Vector3.zero;
        */

        // Reset puzzle completion state
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
                if (hit.collider.CompareTag("Wall"))
                {
                    continue; //This direction is blocked by a wall
                }

                if (hit.collider.CompareTag("Box"))
                {
                    continue; //Treat other boxes as blocking
                }

                if (hit.collider.CompareTag("Floor"))
                {
                    return false; //This direction is free
                }
            }
            else
            {
                // Nothing hit, so it's empty space (e.g., maybe a gap or untagged space)
                return false;
            }
        }

        return true; // All directions blocked
    }


}
