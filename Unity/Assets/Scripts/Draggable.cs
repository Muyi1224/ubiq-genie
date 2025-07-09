using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Draggable : MonoBehaviour
{
    private bool isDragging = false;
    private float distanceToCamera;

    // ��갴�¿�ʼ�϶�
    void OnMouseDown()
    {
        distanceToCamera = Vector3.Distance(transform.position, Camera.main.transform.position);
        isDragging = true;
    }

    // ���̧������϶�
    void OnMouseUp()
    {
        isDragging = false;
    }

    //����϶��߼� + ���� F ɾ��
    void Update()
    {
        if (isDragging)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 target = ray.GetPoint(distanceToCamera);
            transform.position = target;

            if (Input.GetKeyDown(KeyCode.F))
            {
                Destroy(gameObject);
            }
        }
    }

    // XR ����������ɾ�������� Activate ����� trigger��
    public void DeleteViaXR(BaseInteractionEventArgs args)
    {
        Destroy(gameObject);
    }
}
