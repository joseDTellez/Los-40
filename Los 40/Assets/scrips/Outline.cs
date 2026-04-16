using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Outline : MonoBehaviour
{
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    public enum Mode
    {
        OutlineAll,           // Siempre visible (atraviesa paredes)
        OutlineVisible,       // Solo lo que la cámara ve directamente
        OutlineHidden,        // Solo lo que está detrás de algo
        OutlineAndSilhouette, // Outline normal + silueta tras objetos
        SilhouetteOnly        // Solo silueta tras objetos
    }

    public enum InteractionState
    {
        Idle,        // Efecto respiración activo
        Hover,       // Outline fijo y resaltado
        Interacting  // Desactivado o en uso (ancho 0)
    }

    [Header("Interaction Settings")]
    public InteractionState currentState = InteractionState.Idle;

    [Range(0.1f, 5f)]
    public float pulseSpeed = 1.5f;
    [SerializeField, Range(0f, 10f)]
    private float minPulseWidth = 1.5f;
    [SerializeField, Range(0f, 10f)]
    private float maxPulseWidth = 4f;
    [SerializeField, Range(0f, 15f)]
    private float hoverWidth = 7f;

    [Header("Base Settings")]
    [SerializeField] private Mode outlineMode = Mode.OutlineAll; // Por defecto "Siempre visible"
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField, Range(0f, 10f)] private float outlineWidth = 2f;

    [Header("Optional")]
    [SerializeField] private bool precomputeOutline;
    [SerializeField, HideInInspector] private List<Mesh> bakeKeys = new List<Mesh>();
    [SerializeField, HideInInspector] private List<ListVector3> bakeValues = new List<ListVector3>();

    private Renderer[] renderers;
    private Material outlineMaskMaterial;
    private Material outlineFillMaterial;
    private bool needsUpdate;

    [Serializable]
    private class ListVector3
    {
        public List<Vector3> data;
    }

    public void SetState(InteractionState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        needsUpdate = true;
    }

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        // Carga de materiales desde la carpeta Resources
        outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
        outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

        if (outlineMaskMaterial == null || outlineFillMaterial == null)
        {
            Debug.LogError("No se encontraron los materiales de Outline en Resources/Materials. Asegúrate de que existan.");
            return;
        }

        outlineMaskMaterial.name = "OutlineMask (Instance)";
        outlineFillMaterial.name = "OutlineFill (Instance)";

        LoadSmoothNormals();
        needsUpdate = true;
    }

    void OnEnable()
    {
        if (renderers == null) return;

        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials.ToList();
            if (!materials.Contains(outlineMaskMaterial)) materials.Add(outlineMaskMaterial);
            if (!materials.Contains(outlineFillMaterial)) materials.Add(outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
        needsUpdate = true;
    }

    void Update()
    {
        // Si hay cambios pendientes (como el modo o el color), los aplicamos
        if (needsUpdate)
        {
            UpdateMaterialProperties();
            needsUpdate = false;
        }

        // Lógica de animación si está en IDLE
        if (currentState == InteractionState.Idle)
        {
            float lerp = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            float currentWidth = Mathf.Lerp(minPulseWidth, maxPulseWidth, lerp);

            outlineFillMaterial.SetFloat("_OutlineWidth", currentWidth);
            outlineFillMaterial.SetColor("_OutlineColor", outlineColor);
        }
    }

    void OnDisable()
    {
        if (renderers == null) return;

        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials.ToList();
            materials.Remove(outlineMaskMaterial);
            materials.Remove(outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void OnDestroy()
    {
        if (outlineMaskMaterial != null) Destroy(outlineMaskMaterial);
        if (outlineFillMaterial != null) Destroy(outlineFillMaterial);
    }

    void OnValidate()
    {
        needsUpdate = true;
        if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count)
        {
            bakeKeys.Clear();
            bakeValues.Clear();
        }
        if (precomputeOutline && bakeKeys.Count == 0)
        {
            Bake();
        }
    }

    void UpdateMaterialProperties()
    {
        if (outlineFillMaterial == null || outlineMaskMaterial == null) return;

        outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

        float targetWidth = outlineWidth;
        if (currentState == InteractionState.Interacting) targetWidth = 0f;
        else if (currentState == InteractionState.Hover) targetWidth = hoverWidth;

        // Configuración de la visibilidad a través de ZTest
        switch (outlineMode)
        {
            case Mode.OutlineAll:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                break;
            case Mode.OutlineVisible:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                break;
            case Mode.OutlineHidden:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                break;
            case Mode.OutlineAndSilhouette:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                break;
            case Mode.SilhouetteOnly:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                targetWidth = 0f;
                break;
        }

        // Si no es Idle, aplicamos el ancho fijo (Hover o Default)
        if (currentState != InteractionState.Idle)
        {
            outlineFillMaterial.SetFloat("_OutlineWidth", targetWidth);
        }
    }

    // --- MÉTODOS DE PROCESAMIENTO DE MALLA ---

    void Bake()
    {
        var bakedMeshes = new HashSet<Mesh>();
        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (!bakedMeshes.Add(meshFilter.sharedMesh)) continue;
            var smoothNormals = SmoothNormals(meshFilter.sharedMesh);
            bakeKeys.Add(meshFilter.sharedMesh);
            bakeValues.Add(new ListVector3() { data = smoothNormals });
        }
    }

    void LoadSmoothNormals()
    {
        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (!registeredMeshes.Add(meshFilter.sharedMesh)) continue;
            var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
            var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);
            meshFilter.sharedMesh.SetUVs(3, smoothNormals);
            var renderer = meshFilter.GetComponent<Renderer>();
            if (renderer != null) CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials);
        }

        foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!registeredMeshes.Add(skinnedMeshRenderer.sharedMesh)) continue;
            skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];
            CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
        }
    }

    List<Vector3> SmoothNormals(Mesh mesh)
    {
        var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);
        var smoothNormals = new List<Vector3>(mesh.normals);
        foreach (var group in groups)
        {
            if (group.Count() == 1) continue;
            var smoothNormal = Vector3.zero;
            foreach (var pair in group) smoothNormal += smoothNormals[pair.Value];
            smoothNormal.Normalize();
            foreach (var pair in group) smoothNormals[pair.Value] = smoothNormal;
        }
        return smoothNormals;
    }

    void CombineSubmeshes(Mesh mesh, Material[] materials)
    {
        if (mesh.subMeshCount == 1 || mesh.subMeshCount > materials.Length) return;
        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }
}