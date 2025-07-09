using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class DeleteOnButton : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Destroy(gameObject);
        }
    }

    public void DeleteFromXR(ActivateEventArgs args)
    {
        Destroy(gameObject);
    }
}
