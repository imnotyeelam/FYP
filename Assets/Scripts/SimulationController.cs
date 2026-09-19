using UnityEngine;

public class SimulationController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float horizontalStep = 2f;
    public float verticalStep = 1f;

    public void RecursiveCall()
    {
        // Move forward and downward to represent going deeper
        // into the recursive call.
        transform.position += new Vector3(
            horizontalStep,
            -verticalStep,
            0f
        );

        Debug.Log("Recursive call!");
    }

    public void ReachBaseCase()
    {
        // For now, just make the object slightly bigger
        // so we can clearly see that the base case was reached.
        transform.localScale = Vector3.one * 1.5f;

        Debug.Log("Base case reached!");
    }

    public void ReturnFromCall()
    {
        // Move backward and upward to represent returning
        // from the recursive call.
        transform.position += new Vector3(
            -horizontalStep,
            verticalStep,
            0f
        );

        // Reset size after leaving the base case.
        transform.localScale = Vector3.one;

        Debug.Log("Returning from recursive call!");
    }
}