using UnityEngine;

public class Draggable : MonoBehaviour
{
    private bool isDragging = false;
    private float distanceToCamera;

    void OnMouseDown()
    {
        distanceToCamera = Vector3.Distance(transform.position, Camera.main.transform.position);
        isDragging = true;
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    void Update()
    {
        if (isDragging)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 target = ray.GetPoint(distanceToCamera);
            transform.position = target;
        }
    }
}
