
using UnityEngine;
using System.Collections.Generic;

public class SimulationController : MonoBehaviour
{
    [Header("Recursion Settings")]
    public int startingValue = 3;

    [Header("Call Stack Visualization")]
    public CallStackBlock blockPrefab;
    public Transform stackOrigin;
    public float horizontalStep = 2f;
    public float verticalStep = 1f;

    private int currentValue;
    private bool baseCaseReached;
    private bool simulationCompleted;

    private List<CallStackBlock> callStack =
        new List<CallStackBlock>();

    void Start()
    {
        ResetSimulation();
    }

    public void RecursiveCall()
    {
        if (simulationCompleted || baseCaseReached)
        {
            Debug.Log("Cannot make another recursive call.");
            return;
        }

        if (currentValue <= 0)
        {
            Debug.Log("Base case reached. Use 'base'.");
            return;
        }

        currentValue--;
        SpawnBlock(currentValue);

        Debug.Log("Recursive call: countdown(" +
                  currentValue + ")");
    }

    public void ReachBaseCase()
    {
        if (simulationCompleted || baseCaseReached)
        {
            Debug.Log("Base case already processed.");
            return;
        }

        if (currentValue != 0)
        {
            Debug.Log("Not yet! Current n = " +
                      currentValue);
            return;
        }

        baseCaseReached = true;

        if (callStack.Count > 0)
            callStack[callStack.Count - 1].SetBaseCase();

        Debug.Log("Correct! Base case reached.");
    }

    public void ReturnFromCall()
    {
        if (!baseCaseReached || simulationCompleted)
        {
            Debug.Log("Cannot return yet.");
            return;
        }

        if (callStack.Count == 0)
            return;

        CallStackBlock topBlock =
            callStack[callStack.Count - 1];

        callStack.RemoveAt(callStack.Count - 1);
        Destroy(topBlock.gameObject);

        currentValue++;

        if (callStack.Count > 0)
        {
            callStack[callStack.Count - 1]
                .SetReturning();
        }

        if (callStack.Count == 0)
        {
            simulationCompleted = true;
            Debug.Log("Simulation completed!");
        }
        else
        {
            Debug.Log("Returning to countdown(" +
                      (currentValue - 1) + ")");
        }
    }

    void SpawnBlock(int n)
    {
        if (blockPrefab == null)
        {
            Debug.LogError("Block Prefab is not assigned!");
            return;
        }

        Vector3 position = stackOrigin != null
            ? stackOrigin.position
            : transform.position;

        int depth = callStack.Count;

        position += new Vector3(
            depth * horizontalStep,
            -depth * verticalStep,
            0f
        );

        CallStackBlock newBlock =
            Instantiate(blockPrefab, position,
                        Quaternion.identity);

        newBlock.Initialize(n);
        callStack.Add(newBlock);
    }

    public void ResetSimulation()
    {
        foreach (CallStackBlock block in callStack)
        {
            if (block != null)
                Destroy(block.gameObject);
        }

        callStack.Clear();

        currentValue = Mathf.Max(0, startingValue);
        baseCaseReached = false;
        simulationCompleted = false;

        SpawnBlock(currentValue);

        Debug.Log("Simulation started: countdown(" +
                  currentValue + ")");
    }
}
