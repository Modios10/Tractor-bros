using UnityEngine;
using System.Collections.Generic;

public class LevelSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject grainPrefab;
    [SerializeField] private GameObject bombPrefab;

    [Header("Spawn Area")]
    [SerializeField] private Vector2 areaMin = new Vector2(-7f, -4f);
    [SerializeField] private Vector2 areaMax = new Vector2(7f, 4f);
    [SerializeField] private float avoidRadiusAroundPlayer = 1.5f;
    [SerializeField] private float bombAvoidRadiusAroundPlayer = 4f;
    [SerializeField] private Transform player;

    [Header("Optional Parents")]
    [SerializeField] private Transform grainParent;
    [SerializeField] private Transform bombParent;

    [Header("Bomb Offset")]
    [SerializeField] private float bombSpawnZ = 0f;

    [Header("Cleanup")]
    [SerializeField] private bool clearTaggedObjectsOnSetup = true;

    private readonly List<GameObject> spawnedGrains = new List<GameObject>();
    private readonly List<GameObject> spawnedBombs = new List<GameObject>();

    public void SetupLevel(LevelManager.LevelConfig config)
    {
        ClearSpawnedLists();
        ClearChildren(grainParent);
        ClearChildren(bombParent);

        if (clearTaggedObjectsOnSetup)
        {
            ClearByTag("Grain");
            ClearByTag("Obstaculo");
        }

        SpawnGrains(config.grainCount);
        SpawnBombs(config.bombCount);

        Debug.Log("Spawner setup -> grains: " + config.grainCount + ", bombs: " + config.bombCount);
    }

    private void SpawnGrains(int count)
    {
        if (grainPrefab == null || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 position = GetRandomSpawnPosition();
            GameObject grain = Instantiate(grainPrefab, position, Quaternion.identity, grainParent);
            grain.tag = "Grain";
            spawnedGrains.Add(grain);
        }
    }

    private void SpawnBombs(int count)
    {
        if (bombPrefab == null || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 position = GetRandomSpawnPosition(bombAvoidRadiusAroundPlayer);
            position.z = bombSpawnZ;
            GameObject bomb = Instantiate(bombPrefab, position, Quaternion.identity, bombParent);
            spawnedBombs.Add(bomb);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        return GetRandomSpawnPosition(avoidRadiusAroundPlayer);
    }

    private Vector3 GetRandomSpawnPosition(float minimumDistanceFromPlayer)
    {
        Vector3 position = Vector3.zero;
        int attempts = 0;

        while (attempts < 20)
        {
            position.x = Random.Range(areaMin.x, areaMax.x);
            position.y = Random.Range(areaMin.y, areaMax.y);
            position.z = 0f;

            if (player == null)
            {
                break;
            }

            float distance = Vector2.Distance(player.position, position);
            if (distance >= minimumDistanceFromPlayer)
            {
                break;
            }

            attempts++;
        }

        return position;
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void ClearSpawnedLists()
    {
        for (int i = spawnedGrains.Count - 1; i >= 0; i--)
        {
            if (spawnedGrains[i] != null)
            {
                Destroy(spawnedGrains[i]);
            }
        }

        for (int i = spawnedBombs.Count - 1; i >= 0; i--)
        {
            if (spawnedBombs[i] != null)
            {
                Destroy(spawnedBombs[i]);
            }
        }

        spawnedGrains.Clear();
        spawnedBombs.Clear();
    }

    private void ClearByTag(string tagName)
    {
        GameObject[] tagged = GameObject.FindGameObjectsWithTag(tagName);
        for (int i = 0; i < tagged.Length; i++)
        {
            if (tagged[i].GetComponentInParent<Canvas>() != null)
            {
                continue;
            }

            Destroy(tagged[i]);
        }
    }
}
