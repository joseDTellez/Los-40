using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OutlineVR : MonoBehaviour // <-- Ahora se llama OutlineVR para no chocar con Unity
{
    public enum InteractionState { Idle, Hover, Interacting }

    [Header("Configuración de Interacción")]
    public InteractionState currentState = InteractionState.Idle;
    [Range(0.1f, 5f)] public float pulseSpeed = 1.5f;

    [Header("Colores (Configúralos en el Inspector)")]
    public Color colorNaranja = new Color(1f, 0.5f, 0f); // Naranja por defecto

    [Header("Anchos del Borde")]
    public float minPulseWidth = 1.5f;
    public float maxPulseWidth = 4f;
    public float hoverWidth = 7f;

    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    private static readonly int WidthID = Shader.PropertyToID("_OutlineWidth");
    private static readonly int ColorID = Shader.PropertyToID("_OutlineColor");

    // Métodos para Cardboard (SendMessage)
    public void SetState(InteractionState newState) => currentState = newState;
    public void OnPointerEnter() => SetState(InteractionState.Hover);
    public void OnPointerExit() => SetState(InteractionState.Idle);

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        Material maskBase = Resources.Load<Material>("Materials/OutlineMask");
        Material fillBase = Resources.Load<Material>("Materials/OutlineFill");

        if (maskBase == null || fillBase == null)
        {
            Debug.LogError("Faltan materiales en Resources/Materials");
            enabled = false;
            return;
        }

        foreach (var r in renderers)
        {
            List<Material> mats = new List<Material>(r.sharedMaterials);
            if (!mats.Contains(maskBase)) mats.Add(maskBase);
            if (!mats.Contains(fillBase)) mats.Add(fillBase);
            r.sharedMaterials = mats.ToArray();
        }

        // Esto es lo que hace que el borde rodee TODO el objeto (Smooth Normals)
        LoadSmoothNormals();
    }

    void Update()
    {
        float finalWidth = 0f;

        // Siempre usamos colorNaranja para que no salga blanco
        if (currentState == InteractionState.Idle)
        {
            float lerp = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            finalWidth = Mathf.Lerp(minPulseWidth, maxPulseWidth, lerp);
        }
        else if (currentState == InteractionState.Hover)
        {
            finalWidth = hoverWidth;
        }

        ApplyProperties(finalWidth, colorNaranja);
    }

    private void ApplyProperties(float width, Color color)
    {
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetFloat(WidthID, width);
            propBlock.SetColor(ColorID, color);
            r.SetPropertyBlock(propBlock);
        }
    }

    // --- TU MATEMÁTICA PARA EL ÁREA COMPLETA ---
    void LoadSmoothNormals()
    {
        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (!registeredMeshes.Add(meshFilter.sharedMesh)) continue;
            meshFilter.sharedMesh.SetUVs(3, SmoothNormals(meshFilter.sharedMesh));
        }
        foreach (var skinnedMesh in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!registeredMeshes.Add(skinnedMesh.sharedMesh)) continue;
            skinnedMesh.sharedMesh.SetUVs(3, SmoothNormals(skinnedMesh.sharedMesh));
        }
    }

    List<Vector3> SmoothNormals(Mesh mesh)
    {
        var dict = new Dictionary<Vector3, Vector3>();
        foreach (var v in mesh.vertices) if (!dict.ContainsKey(v)) dict.Add(v, Vector3.zero);
        var normals = mesh.normals;
        var vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) dict[vertices[i]] += normals[i];
        var smoothNormals = new List<Vector3>(normals);
        for (int i = 0; i < smoothNormals.Count; i++) smoothNormals[i] = dict[vertices[i]].normalized;
        return smoothNormals;
    }
}