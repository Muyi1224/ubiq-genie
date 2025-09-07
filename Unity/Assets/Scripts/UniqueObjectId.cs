using UnityEngine;
using System;

public class UniqueObjectId : MonoBehaviour
{
    public string objectId;

    void Awake()
    {
        if (string.IsNullOrEmpty(objectId))
        {
            objectId = Guid.NewGuid().ToString(); // generate unique UUID
        }
    }
}

