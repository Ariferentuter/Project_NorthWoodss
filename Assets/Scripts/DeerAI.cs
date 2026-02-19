using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DeerAI : MonoBehaviour
{
    public float roamRadius = 20f;
    public float roamDelay = 5f;

    private NavMeshAgent agent;
    private float timer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (!agent.isOnNavMesh)
        {
            UnityEngine.Debug.LogError("Agent NavMesh üzerinde deðil! Geyiði mavi alan üstüne koy.");
            enabled = false; // Script durur ama geyik kaybolmaz
            return;
        }

        timer = roamDelay;
    }

    void Update()
    {
        if (!agent.isOnNavMesh)
            return;

        timer += Time.deltaTime;

        if (timer >= roamDelay)
        {
            Vector3 newPos = GetRandomNavPoint(transform.position, roamRadius);

            if (newPos != Vector3.zero)
            {
                agent.SetDestination(newPos);
            }

            timer = 0f;
        }
    }

    Vector3 GetRandomNavPoint(Vector3 origin, float distance)
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * distance;
        randomDirection.y = 0f;
        randomDirection += origin;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, distance, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return Vector3.zero;
    }
}