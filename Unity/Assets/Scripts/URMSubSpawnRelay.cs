using UnityEngine;

public class URMSubSpawnRelay : MonoBehaviour
{
    [Header("要生成的条目名（与 spawnableItems.name 对应）")]
    public string itemName;

    [Header("可选：从哪个变换前方生成")]
    public Transform spawnFrom; // 比如右手控制器或你的射线起点

    public void OnClick()
    {
        if (string.IsNullOrEmpty(itemName)) return;

        if (spawnFrom)
            SpawnMenu.Instance?.SpawnByName(itemName);
    }
}