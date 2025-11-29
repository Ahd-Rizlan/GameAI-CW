using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    Grid grid;

    void Awake()
    {
        grid = GetComponent<Grid>();
    }

    public void FindPath(Vector3 startPos, Vector3 targetPos, SingleGunner requestor)
    {
        StartCoroutine(FindPathCoroutine(startPos, targetPos, requestor));
    }

    IEnumerator FindPathCoroutine(Vector3 startPos, Vector3 targetPos, SingleGunner requestor)
    {
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;

        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        
        if (startNode.walkable && targetNode.walkable)
        {
            // BFS uses a Queue (First In, First Out)
            Queue<Node> queue = new Queue<Node>();

            // HashSet tracks visited nodes efficiently
            HashSet<Node> visited = new HashSet<Node>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                Node currentNode = queue.Dequeue();

                
                if (currentNode == targetNode)
                {
                    pathSuccess = true;
                    break;
                }

                foreach (Node neighbour in grid.GetNeighbours(currentNode))
                {
                    
                    if (!neighbour.walkable || visited.Contains(neighbour))
                    {
                        continue;
                    }

                    
                    visited.Add(neighbour);

                    
                    neighbour.parent = currentNode;

                   
                    queue.Enqueue(neighbour);
                }
            }
        }

        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
        }

        
        requestor.OnPathFound(waypoints, pathSuccess);
        yield return null;
    }

    Vector3[] RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();

        List<Vector3> waypoints = new List<Vector3>();
        for (int i = 0; i < path.Count; i++)
        {
            waypoints.Add(path[i].worldPosition);
        }
        return waypoints.ToArray();
    }
}