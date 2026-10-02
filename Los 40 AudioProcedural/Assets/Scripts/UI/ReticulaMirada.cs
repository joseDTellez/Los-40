using UnityEngine;

/// <summary>
/// Reticula (mira) en el centro de la vista. Basada en el Reticle Pointer de Google Cardboard,
/// pero solo dibuja: la deteccion de objetos la hace GazeSystem, y la reticula crece cuando
/// GazeSystem tiene un objeto interactivo enfrente.
/// </summary>
public class ReticulaMirada : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Range(-32767, 32767)]
    public int ReticleSortingOrder = 32767;

    [Tooltip("Sistema de mirada del jugador. Si se deja vacio se busca en la escena.")]
    public GazeSystem gazeSystem;

    private const float _RETICLE_MIN_INNER_ANGLE = 0.0f;
    private const float _RETICLE_MIN_OUTER_ANGLE = 0.5f;
    private const float _RETICLE_GROWTH_ANGLE = 1.5f;
    private const float _RETICLE_MIN_DISTANCE = 0.45f;
    private const float _RETICLE_MAX_DISTANCE = 20.0f;
    private const int _RETICLE_SEGMENTS = 20;
    private const float _RETICLE_GROWTH_SPEED = 8.0f;

    private static readonly int InnerDiameterId = Shader.PropertyToID("_InnerDiameter");
    private static readonly int OuterDiameterId = Shader.PropertyToID("_OuterDiameter");
    private static readonly int DistanceId = Shader.PropertyToID("_DistanceInMeters");

    private Material _reticleMaterial;
    private float _reticleInnerAngle;
    private float _reticleOuterAngle;
    private float _reticleDistanceInMeters;
    private float _reticleInnerDiameter;
    private float _reticleOuterDiameter;

    private void Start()
    {
        Renderer rendererComponent = GetComponent<Renderer>();
        rendererComponent.sortingOrder = ReticleSortingOrder;
        _reticleMaterial = rendererComponent.material;
        CreateMesh();

        if (gazeSystem == null)
            gazeSystem = FindAnyObjectByType<GazeSystem>();
    }

    private void Update()
    {
        if (gazeSystem != null && gazeSystem.HayObjetivo)
        {
            _reticleDistanceInMeters = gazeSystem.DistanciaObjetivo;
            _reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE + _RETICLE_GROWTH_ANGLE;
            _reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE + _RETICLE_GROWTH_ANGLE;
        }
        else
        {
            _reticleDistanceInMeters = _RETICLE_MAX_DISTANCE;
            _reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
            _reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
        }

        UpdateDiameters();
    }

    private void OnDestroy()
    {
        // renderer.material crea una copia del material: hay que liberarla
        if (_reticleMaterial != null) Destroy(_reticleMaterial);
    }

    private void UpdateDiameters()
    {
        _reticleDistanceInMeters = Mathf.Clamp(_reticleDistanceInMeters, _RETICLE_MIN_DISTANCE, _RETICLE_MAX_DISTANCE);

        float inner_half_angle_radians = Mathf.Deg2Rad * _reticleInnerAngle * 0.5f;
        float outer_half_angle_radians = Mathf.Deg2Rad * _reticleOuterAngle * 0.5f;
        float inner_diameter = 2.0f * Mathf.Tan(inner_half_angle_radians);
        float outer_diameter = 2.0f * Mathf.Tan(outer_half_angle_radians);

        _reticleInnerDiameter = Mathf.Lerp(_reticleInnerDiameter, inner_diameter, Time.unscaledDeltaTime * _RETICLE_GROWTH_SPEED);
        _reticleOuterDiameter = Mathf.Lerp(_reticleOuterDiameter, outer_diameter, Time.unscaledDeltaTime * _RETICLE_GROWTH_SPEED);

        _reticleMaterial.SetFloat(InnerDiameterId, _reticleInnerDiameter * _reticleDistanceInMeters);
        _reticleMaterial.SetFloat(OuterDiameterId, _reticleOuterDiameter * _reticleDistanceInMeters);
        _reticleMaterial.SetFloat(DistanceId, _reticleDistanceInMeters);
    }

    private void CreateMesh()
    {
        Mesh mesh = new Mesh();
        gameObject.AddComponent<MeshFilter>().mesh = mesh;
        int segments_count = _RETICLE_SEGMENTS;
        int vertex_count = (segments_count + 1) * 2;
        Vector3[] vertices = new Vector3[vertex_count];
        const float kTwoPi = Mathf.PI * 2.0f;
        int vi = 0;
        for (int si = 0; si <= segments_count; ++si)
        {
            float angle = (float)si / (float)segments_count * kTwoPi;
            float x = Mathf.Sin(angle);
            float y = Mathf.Cos(angle);
            vertices[vi++] = new Vector3(x, y, 0.0f);
            vertices[vi++] = new Vector3(x, y, 1.0f);
        }
        int indices_count = (segments_count + 1) * 3 * 2;
        int[] indices = new int[indices_count];
        int vert = 0; int idx = 0;
        for (int si = 0; si < segments_count; ++si)
        {
            indices[idx++] = vert + 1; indices[idx++] = vert; indices[idx++] = vert + 2;
            indices[idx++] = vert + 1; indices[idx++] = vert + 2; indices[idx++] = vert + 3;
            vert += 2;
        }
        mesh.vertices = vertices;
        mesh.triangles = indices;
        // El shader desplaza los vertices hasta _DistanceInMeters: ampliamos los bounds
        // para que la camara no descarte la reticula por frustum culling.
        mesh.bounds = new Bounds(Vector3.forward * _RETICLE_MAX_DISTANCE * 0.5f, Vector3.one * _RETICLE_MAX_DISTANCE * 2f);
    }
}
