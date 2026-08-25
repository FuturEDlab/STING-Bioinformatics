using UnityEngine;

public class RespawnObject : MonoBehaviour
{
    [SerializeField] private Vector3 spawnPoint;

    void Update()
    {
        if (transform.position.y < -1)
        {
            transform.position = spawnPoint;
        }
    }
}