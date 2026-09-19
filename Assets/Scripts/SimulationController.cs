using UnityEngine;

public class SimulationController : MonoBehaviour
{
    public void MoveForward()
    {
        transform.position += Vector3.forward * 2f;
    }
}