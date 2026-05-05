using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OutlineVR : MonoBehaviour
{
    public enum InteractionState { Idle, Hover, Interacting }

    [Header("Configuración de Interacción")]
    public InteractionState currentState = InteractionState.Idle;
    [Range(0.1f, 5f)] public float pulseSpeed = 1.5f;

    [Header("Colores (Elige tu color aquí)")]
    public Color colorNaranja = new Color(1f, 0.5f, 0f);

    [Header("Anchos del Borde")]
    public float minPulseWidth = 1.5f;
    public float maxPulseWidth = 4f;
    public float hoverWidth = 7f;

    private Renderer[] originalRenderers;
    private List<Renderer> outlineRenderers = new List<Renderer>();
    private MaterialPropertyBlock propBlock;
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    private static readonly int WidthID = Shader.PropertyToID("_OutlineWidth");
    private static readonly int ColorID = Shader.PropertyToID("_OutlineColor");

    public void SetState(InteractionState newState) => currentState = newState;
    public void OnPointerEnter() => SetState(InteractionState.Hover);
    public void OnPointerExit() => SetState(InteractionState.Idle);

    void Awake()
    {
        originalRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        Material fillBase = Resources.Load<Material>("Materials/OutlineFill");
        if (fillBase == null)
        {
            Debug.LogError("Falta el material OutlineFill en Resources/Materials");
            enabled = false;
            return;
        }

        LoadSmoothNormals();

        foreach (var r in originalRenderers)
        {
            // Evitamos clonar objetos que ya sean clones
            if (r.gameObject.name.EndsWith("_OutlineClone")) continue;

            GameObject outlineObj = new GameObject(r.gameObject.name + "_OutlineClone");
            outlineObj.transform.SetParent(r.transform, false);
            outlineObj.transform.localPosition = Vector3.zero;
            outlineObj.transform.localRotation = Quaternion.identity;
            outlineObj.transform.localScale = Vector3.one;

            Renderer outlineRnd = null;

            if (r is MeshRenderer meshRenderer)
            {
                var mf = r.GetComponent<MeshFilter>();
                // Blindaje: Verificar que exista la malla antes de copiar
                if (mf == null || mf.sharedMesh == null)
                {
                    Destroy(outlineObj);
                    continue;
                }

                var newMf = outlineObj.AddComponent<MeshFilter>();
                newMf.sharedMesh = mf.sharedMesh;
                outlineRnd = outlineObj.AddComponent<MeshRenderer>();
            }
            else if (r is SkinnedMeshRenderer skinned)
            {
                // Blindaje: Verificar malla y arreglos internos
                if (skinned.sharedMesh == null)
                {
                    Destroy(outlineObj);
                    continue;
                }

                var newSkinned = outlineObj.AddComponent<SkinnedMeshRenderer>();
                newSkinned.sharedMesh = skinned.sharedMesh;

                if (skinned.rootBone != null) newSkinned.rootBone = skinned.rootBone;

                // ¡AQUÍ OCURRÍA EL ERROR! Unity colapsa si le asignas un array de huesos nulo.
                if (skinned.bones != null && skinned.bones.Length > 0)
                {
                    newSkinned.bones = skinned.bones;
                }

                outlineRnd = newSkinned;
            }

            if (outlineRnd != null && r.sharedMaterials != null)
            {
                // Asignamos el material base a todas las caras sin riesgo de nulls
                Material[] outlineMats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < outlineMats.Length; i++)
                {
                    outlineMats[i] = fillBase;
                }
                outlineRnd.sharedMaterials = outlineMats;

                // Apagamos sombras para no saturar el rendimiento en el visor
                outlineRnd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outlineRnd.receiveShadows = false;
                outlineRenderers.Add(outlineRnd);
            }
        }
    }

    void Update()
    {
        float finalWidth = 0f;

        if (currentState == InteractionState.Idle)
        {
            float lerp = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            finalWidth = Mathf.Lerp(minPulseWidth, maxPulseWidth, lerp);
        }
        else if (currentState == InteractionState.Hover)
        {
            finalWidth = hoverWidth;
        }

        foreach (var rnd in outlineRenderers)
        {
            if (rnd != null)
            {
                rnd.GetPropertyBlock(propBlock);
                propBlock.SetFloat(WidthID, finalWidth);
                propBlock.SetColor(ColorID, colorNaranja);
                rnd.SetPropertyBlock(propBlock);
            }
        }
    }

    void LoadSmoothNormals()
    {
        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            Mesh mesh = meshFilter.sharedMesh;
            // Blindaje contra mallas vacías o sin normales
            if (mesh == null || mesh.vertexCount == 0 || mesh.normals == null || mesh.normals.Length == 0 || !registeredMeshes.Add(mesh)) continue;

            try { mesh.SetUVs(3, SmoothNormals(mesh)); }
            catch { /* Ignoramos mallas rotas que no permiten UVs */ }
        }

        foreach (var skinnedMesh in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Mesh mesh = skinnedMesh.sharedMesh;
            // Blindaje contra mallas vacías o sin normales
            if (mesh == null || mesh.vertexCount == 0 || mesh.normals == null || mesh.normals.Length == 0 || !registeredMeshes.Add(mesh)) continue;

            try { mesh.SetUVs(3, SmoothNormals(mesh)); }
            catch { /* Ignoramos mallas rotas que no permiten UVs */ }
        }
    }

    List<Vector3> SmoothNormals(Mesh mesh)
    {
        var dict = new Dictionary<Vector3, Vector3>();
        var vertices = mesh.vertices;
        var normals = mesh.normals;

        for (int i = 0; i < vertices.Length; i++)
        {
            if (!dict.ContainsKey(vertices[i])) dict.Add(vertices[i], Vector3.zero);
            dict[vertices[i]] += normals[i];
        }

        var smoothNormals = new List<Vector3>(vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            smoothNormals.Add(dict[vertices[i]].normalized);
        }
        return smoothNormals;
    }
}