using UnityEngine;
using UnityEngine.AI;

public class RandomNavMeshWanderAuto : MonoBehaviour
{
    public float waitTime = 2f;   // Tiempo de espera entre destinos
    private NavMeshAgent agent;
    private float timer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = waitTime;
        MoveToRandomPoint();
    }

    void Update()
    {
        // Cuando llega a destino, espera un poco y elige otro punto
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                MoveToRandomPoint();
                timer = waitTime;
            }
        }
    }

    void MoveToRandomPoint()
    {
        // Obtener la geometría del NavMesh actual
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        // Elegir un triángulo aleatorio
        int vertexIndex = Random.Range(0, navMeshData.indices.Length - 3);

        // Calcular un punto aleatorio dentro de ese triángulo
        Vector3 point = RandomPointInTriangle(
            navMeshData.vertices[navMeshData.indices[vertexIndex]],
            navMeshData.vertices[navMeshData.indices[vertexIndex + 1]],
            navMeshData.vertices[navMeshData.indices[vertexIndex + 2]]
        );

        // Enviar al agente al punto elegido
        agent.SetDestination(point);
    }

    // Genera un punto aleatorio dentro de un triángulo 3D
    Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
    }
}
