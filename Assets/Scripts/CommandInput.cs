using UnityEngine;
using TMPro;

public class CommandInput : MonoBehaviour
{
    public TMP_InputField inputField;
    public SimulationController simulationController;

    public void SubmitCommand()
    {
        string command = inputField.text.Trim().ToLower();

        if (command.Contains("move"))
        {
            simulationController.MoveForward();
            Debug.Log("Move command detected!");
        }
        else
        {
            Debug.Log("Command not recognized.");
        }

        inputField.text = "";
        inputField.ActivateInputField();
    }
}