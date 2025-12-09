using UnityEngine;
using UnityEngine.AI;

public class RandomNavMeshWanderAuto_NoWaitMultiplayer : MonoBehaviour
{
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        MoveToRandomPoint();
    }

    void Update()
    {
        // Si está a menos de 1 metro del destino, elige un nuevo punto
        if (!agent.pathPending && agent.remainingDistance <= 1f)
        {
            MoveToRandomPoint();
        }
    }

    void MoveToRandomPoint()
    {
        // Obtener toda la geometría del NavMesh
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        // Elegir un triángulo aleatorio
        int vertexIndex = Random.Range(0, navMeshData.indices.Length - 3);

        // Calcular un punto aleatorio dentro de ese triángulo
        Vector3 point = RandomPointInTriangle(
            navMeshData.vertices[navMeshData.indices[vertexIndex]],
            navMeshData.vertices[navMeshData.indices[vertexIndex + 1]],
            navMeshData.vertices[navMeshData.indices[vertexIndex + 2]]
        );

        // Enviar al agente hacia ese punto
        agent.SetDestination(point);
    }

    // Genera un punto aleatorio dentro de un triángulo
    Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
    }
}
