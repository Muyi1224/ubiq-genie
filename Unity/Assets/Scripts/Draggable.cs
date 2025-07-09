using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Draggable : MonoBehaviour
{
    private bool isDragging = false;
    private float distanceToCamera;

    // 鼠标按下开始拖动
    void OnMouseDown()
    {
        distanceToCamera = Vector3.Distance(transform.position, Camera.main.transform.position);
        isDragging = true;
    }

    // 鼠标抬起结束拖动
    void OnMouseUp()
    {
        isDragging = false;
    }

    //鼠标拖动逻辑 + 键盘 F 删除
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

    // XR 控制器触发删除（例如 Activate 输入绑定 trigger）
    public void DeleteViaXR(BaseInteractionEventArgs args)
    {
        Destroy(gameObject);
    }
}
