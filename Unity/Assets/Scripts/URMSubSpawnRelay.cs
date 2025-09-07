using UnityEngine;

public class URMSubSpawnRelay : MonoBehaviour
{
    public string itemName;
    public Transform spawnFrom;

    public void OnClick()
    {
        if (string.IsNullOrEmpty(itemName)) return;
    }
}