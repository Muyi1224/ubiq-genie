using UnityEngine;

public class URMSubSpawnRelay : MonoBehaviour
{
    [Header("Ҫ���ɵ���Ŀ������ spawnableItems.name ��Ӧ��")]
    public string itemName;

    [Header("��ѡ�����ĸ��任ǰ������")]
    public Transform spawnFrom; // �������ֿ�����������������

    public void OnClick()
    {
        if (string.IsNullOrEmpty(itemName)) return;

        if (spawnFrom)
            SpawnMenu.Instance?.SpawnByName(itemName);
    }
}