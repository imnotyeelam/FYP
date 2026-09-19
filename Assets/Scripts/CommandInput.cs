using UnityEngine;
using TMPro;

public class CommandInput : MonoBehaviour
{
    public TMP_InputField inputField;
    public SimulationController simulationController;

    public void SubmitCommand()
    {
        string command = inputField.text.Trim().ToLower();

        if (command.Contains("call"))
        {
            simulationController.RecursiveCall();
        }
        else if (command.Contains("base"))
        {
            simulationController.ReachBaseCase();
        }
        else if (command.Contains("return"))
        {
            simulationController.ReturnFromCall();
        }
        else
        {
            Debug.Log("Command not recognized.");
        }

        inputField.text = "";
        inputField.ActivateInputField();
    }
}