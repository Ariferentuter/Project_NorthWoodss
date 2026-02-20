using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DeerAI : MonoBehaviour
{
    public float roamRadius = 25f;
    public float roamDelay = 4f;

    private NavMeshAgent agent;
    private float timer;
    private Terrain terrain;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        terrain = Terrain.activeTerrain;
    }

    void Start()
    {
        SnapToNavMesh();
        timer = roamDelay;
    }

    void Update()
    {
        if (!agent.isOnNavMesh)
        {
            SnapToNavMesh();
            return;
        }

        timer += Time.deltaTime;

        if (timer >= roamDelay)
        {
            MoveRandom();
            timer = 0f;
        }
    }

    void SnapToNavMesh()
    {
        if (terrain != null)
        {
            Vector3 p = transform.position;
            p.y = terrain.SampleHeight(p);
            transform.position = p;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 20f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            UnityEngine.Debug.LogError("NavMesh bulunamadý! Bake kontrol et.");
        }
    }

    void MoveRandom()
    {
        for (int i = 0; i < 40; i++)
        {
            Vector3 randomPos =
                transform.position +
                UnityEngine.Random.insideUnitSphere * roamRadius;

            randomPos.y = transform.position.y;

            NavMeshHit hit;

            if (NavMesh.SamplePosition(randomPos, out hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        UnityEngine.Debug.LogWarning("Uygun hedef bulunamadý.");
    }
}