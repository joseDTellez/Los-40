//-----------------------------------------------------------------------
// <copyright file="CardboardReticlePointer.cs" company="Google LLC">
// Copyright 2023 Google LLC
// </copyright>
//-----------------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Draws a circular reticle in front of any object that the user points at.
/// Versión modificada con Filtro de Estabilidad para evitar parpadeo en móviles.
/// </summary>
public class CardboardReticlePointer : MonoBehaviour
{
    [Range(-32767, 32767)]
    public int ReticleSortingOrder = 32767;

    public LayerMask ReticleInteractionLayerMask = 1 << _RETICLE_INTERACTION_DEFAULT_LAYER;

    private const int _RETICLE_INTERACTION_DEFAULT_LAYER = 8;
    private const float _RETICLE_MIN_INNER_ANGLE = 0.0f;
    private const float _RETICLE_MIN_OUTER_ANGLE = 0.5f;
    private const float _RETICLE_GROWTH_ANGLE = 1.5f;
    private const float _RETICLE_MIN_DISTANCE = 0.45f;
    private const float _RETICLE_MAX_DISTANCE = 20.0f;
    private const int _RETICLE_SEGMENTS = 20;
    private const float _RETICLE_GROWTH_SPEED = 8.0f;

    // --- NUEVAS VARIABLES PARA ESTABILIDAD ---
    private float _lostObjectTimer = 0f;
    [Tooltip("Tiempo de gracia para ignorar parpadeos del sensor (segundos)")]
    private const float _STABILITY_DELAY = 0.15f;
    // -----------------------------------------

    private GameObject _gazedAtObject = null;
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
    }

    private void Update()
    {
        RaycastHit hit;
        // Lanzamos el Raycast
        bool hitSomething = Physics.Raycast(transform.position, transform.forward, out hit, _RETICLE_MAX_DISTANCE);

        if (hitSomething)
        {
            GameObject hitObject = hit.transform.gameObject;

            // Si detectamos un objeto y es diferente al que teníamos
            if (_gazedAtObject != hitObject)
            {
                // Avisamos al objeto anterior que ya no lo vemos
                if (IsInteractive(_gazedAtObject))
                {
                    _gazedAtObject.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
                }

                _gazedAtObject = hitObject;

                // Avisamos al nuevo objeto que lo estamos mirando
                if (IsInteractive(_gazedAtObject))
                {
                    _gazedAtObject.SendMessage("OnPointerEnter", SendMessageOptions.DontRequireReceiver);
                }
            }

            // Si estamos golpeando algo, reseteamos el temporizador de pérdida
            _lostObjectTimer = 0f;
            SetParams(hit.distance, IsInteractive(_gazedAtObject));
        }
        else
        {
            // SI NO GOLPEAMOS NADA: Aplicamos el filtro de estabilidad
            _lostObjectTimer += Time.deltaTime;

            // Solo si pasa el tiempo de gracia, confirmamos que "perdimos" el objeto
            if (_lostObjectTimer >= _STABILITY_DELAY)
            {
                if (IsInteractive(_gazedAtObject))
                {
                    _gazedAtObject.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
                }

                _gazedAtObject = null;
                ResetParams();
            }
        }

        // Detección del botón (Trigger) del VR Box
        if (Google.XR.Cardboard.Api.IsTriggerPressed)
        {
            if (IsInteractive(_gazedAtObject))
            {
                _gazedAtObject.SendMessage("OnPointerClick", SendMessageOptions.DontRequireReceiver);
            }
        }

        UpdateDiameters();
    }

    private void UpdateDiameters()
    {
        _reticleDistanceInMeters = Mathf.Clamp(_reticleDistanceInMeters, _RETICLE_MIN_DISTANCE, _RETICLE_MAX_DISTANCE);

        if (_reticleInnerAngle < _RETICLE_MIN_INNER_ANGLE) _reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
        if (_reticleOuterAngle < _RETICLE_MIN_OUTER_ANGLE) _reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;

        float inner_half_angle_radians = Mathf.Deg2Rad * _reticleInnerAngle * 0.5f;
        float outer_half_angle_radians = Mathf.Deg2Rad * _reticleOuterAngle * 0.5f;

        float inner_diameter = 2.0f * Mathf.Tan(inner_half_angle_radians);
        float outer_diameter = 2.0f * Mathf.Tan(outer_half_angle_radians);

        _reticleInnerDiameter = Mathf.Lerp(_reticleInnerDiameter, inner_diameter, Time.unscaledDeltaTime * _RETICLE_GROWTH_SPEED);
        _reticleOuterDiameter = Mathf.Lerp(_reticleOuterDiameter, outer_diameter, Time.unscaledDeltaTime * _RETICLE_GROWTH_SPEED);

        _reticleMaterial.SetFloat("_InnerDiameter", _reticleInnerDiameter * _reticleDistanceInMeters);
        _reticleMaterial.SetFloat("_OuterDiameter", _reticleOuterDiameter * _reticleDistanceInMeters);
        _reticleMaterial.SetFloat("_DistanceInMeters", _reticleDistanceInMeters);
    }

    private void SetParams(float distance, bool interactive)
    {
        _reticleDistanceInMeters = Mathf.Clamp(distance, _RETICLE_MIN_DISTANCE, _RETICLE_MAX_DISTANCE);
        if (interactive)
        {
            _reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE + _RETICLE_GROWTH_ANGLE;
            _reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE + _RETICLE_GROWTH_ANGLE;
        }
        else
        {
            _reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
            _reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
        }
    }

    private void ResetParams()
    {
        _reticleDistanceInMeters = _RETICLE_MAX_DISTANCE;
        _reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
        _reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
    }

    private void CreateMesh()
    {
        Mesh mesh = new Mesh();
        if (GetComponent<MeshFilter>() == null) gameObject.AddComponent<MeshFilter>();
        GetComponent<MeshFilter>().mesh = mesh;

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
        int vert = 0;
        int idx = 0;
        for (int si = 0; si < segments_count; ++si)
        {
            indices[idx++] = vert + 1;
            indices[idx++] = vert;
            indices[idx++] = vert + 2;
            indices[idx++] = vert + 1;
            indices[idx++] = vert + 2;
            indices[idx++] = vert + 3;
            vert += 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateBounds();
    }

    private bool IsInteractive(GameObject gameObject)
    {
        if (gameObject == null) return false;
        return (1 << gameObject.layer & ReticleInteractionLayerMask) != 0;
    }
}