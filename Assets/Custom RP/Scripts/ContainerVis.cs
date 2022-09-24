using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ContainerVis : MonoBehaviour
{
    public Color edgeColor = Color.cyan;
    public bool showEdge = true;

    void OnDrawGizmosSelected()
    {
        if (showEdge)
        {
            Gizmos.color = edgeColor;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}
