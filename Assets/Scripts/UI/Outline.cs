using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Outline : MonoBehaviour
{
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    [Serializable]
    private class ListVector3
    {
        public List<Vector3> vecList;

        public ListVector3(List<Vector3> list)
        {
            vecList = list;
        }
    }

    [SerializeField]
    private List<Mesh> bakedKeys = new List<Mesh>();
    [SerializeField]
    private List<ListVector3> bakedValues = new List<ListVector3>();

    public Renderer[] renderers;
    public Material maskMaterial;
    public Material fillMaterial;

    void Awake()
    {
        LoadNormals();
    }

    void OnEnable()
    {
        foreach (var renderer in renderers)
        {
            List<Material> materials = renderer.sharedMaterials.ToList();
            materials.Add(fillMaterial);
            materials.Add(maskMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void OnValidate()
    {
        if (bakedKeys.Count != bakedValues.Count)
        {
            bakedValues.Clear();
            bakedKeys.Clear();           
        }
        if (bakedKeys.Count == 0)
        {
            Bake();
        }
    }

    void OnDisable()
    {
        foreach (var renderer in renderers)
        {
            List<Material> materials = renderer.sharedMaterials.ToList();
            materials.Remove(maskMaterial);
            materials.Remove(fillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void Bake()
    {
        HashSet<Mesh> bakedMeshes = new HashSet<Mesh>();
        foreach (MeshFilter meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (!bakedMeshes.Add(meshFilter.sharedMesh))
                continue;
            List<Vector3> smoothedNormals = CalculateNormals(meshFilter.sharedMesh);
            bakedValues.Add(new ListVector3(smoothedNormals));
            bakedKeys.Add(meshFilter.sharedMesh);
        }
    }

    void LoadNormals()
    {
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
        {
            if (!registeredMeshes.Add(filter.sharedMesh))
                continue;
            int ind = bakedKeys.IndexOf(filter.sharedMesh);
            List<Vector3> smoothNormals = (ind >= 0) ? bakedValues[ind].vecList : CalculateNormals(filter.sharedMesh);
            filter.sharedMesh.SetUVs(3, smoothNormals);
        }
        foreach (SkinnedMeshRenderer rend in GetComponentsInChildren<SkinnedMeshRenderer>())
            if (registeredMeshes.Add(rend.sharedMesh))
                rend.sharedMesh.uv4 = new Vector2[rend.sharedMesh.vertexCount];
    }

    List<Vector3> CalculateNormals(Mesh mesh)
    {
        var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);
        List<Vector3> smoothedNormals = new List<Vector3>(mesh.normals);
        Vector3 smoothedNormal = Vector3.zero;
        foreach (var group in groups)
        {
            if (group.Count() == 1)
                continue;
            smoothedNormal = Vector3.zero;
            foreach (var pair in group)
                smoothedNormal += mesh.normals[pair.Value];
            smoothedNormal.Normalize();
            foreach (var pair in group)
                smoothedNormals[pair.Value] = smoothedNormal;
        }
        return smoothedNormals;
    }
}
