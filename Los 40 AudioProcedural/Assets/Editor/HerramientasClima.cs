using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Monta el Área de Clima en la escena abierta y exporta sus wavetables para escucharlas.
public static class HerramientasClima
{
    const string CarpetaMaterial = "Assets/Materials/Clima";
    const string RutaMaterial = CarpetaMaterial + "/GotaLluvia.mat";
    const string RutaMixer = "Assets/MainAudioMixer.mixer";
    const string NombreParque = "park_terrain_1";

    [MenuItem("Los 40/Clima/Crear o actualizar Área de Clima en el parque")]
    public static void CrearDesdeMenu() => Debug.Log(CrearAreaClimaEnParque());

    [MenuItem("Los 40/Clima/Exportar sonidos de clima (WAV)")]
    public static void ExportarDesdeMenu()
    {
        Debug.Log(ExportarTablas());
        EditorUtility.RevealInFinder(Carpeta);
    }

    static string Carpeta => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ComandosClaude"));

    // Crea (o actualiza, si ya existe) el objeto AreaClima con un trigger del tamaño del parque.
    // Si la escena no tenía cambios sin guardar, la guarda al terminar.
    public static string CrearAreaClimaEnParque()
    {
        Scene escena = SceneManager.GetActiveScene();
        bool teniaCambios = escena.isDirty;

        if (!LimitesParque(escena, out Bounds limites, out string origen))
            return "No encontré el parque ni ningún terreno para dimensionar el área.";

        GameObject area = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "AreaClima");
        if (area == null)
        {
            area = new GameObject("AreaClima");
            Undo.RegisterCreatedObjectUndo(area, "Crear Área de Clima");
            SceneManager.MoveGameObjectToScene(area, escena);
        }
        Undo.RecordObject(area.transform, "Área de Clima");
        area.transform.SetPositionAndRotation(limites.center, Quaternion.identity);
        area.transform.localScale = Vector3.one;

        var caja = Obtener<BoxCollider>(area);
        Undo.RecordObject(caja, "Área de Clima");
        caja.isTrigger = true;
        caja.center = Vector3.zero;
        caja.size = new Vector3(limites.size.x, limites.size.y + 40f, limites.size.z);

        AudioMixerGroup ambiente = AssetDatabase.LoadAllAssetsAtPath(RutaMixer).OfType<AudioMixerGroup>()
            .FirstOrDefault(g => g.name == "Ambiente");

        // Lluvia
        GameObject objetoLluvia = Hijo(area, "Lluvia");
        var lluvia = Obtener<SistemaLluvia>(objetoLluvia);
        Undo.RecordObject(lluvia, "Área de Clima");
        lluvia.materialGota = MaterialGota();
        ConfigurarAudio(objetoLluvia, ambiente, variacionPitch: 0.04f, variacionVolumen: 0.25f);

        // Viento
        GameObject objetoViento = Hijo(area, "Viento");
        var viento = Obtener<ProceduralWind>(objetoViento);
        ConfigurarAudio(objetoViento, ambiente, variacionPitch: 0.08f, variacionVolumen: 0.2f);

        var clima = Obtener<AreaClima>(area);
        Undo.RecordObject(clima, "Área de Clima");
        clima.lluvia = lluvia;
        clima.viento = viento;

        foreach (Object o in new Object[] { area, caja, lluvia, viento, clima }) EditorUtility.SetDirty(o);
        EditorSceneManager.MarkSceneDirty(escena);
        string guardado = teniaCambios ? "La escena tenía cambios sin guardar: guarda con Cmd+S." : "Escena guardada.";
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);

        return $"Área de Clima lista en '{escena.name}' (tamaño tomado de {origen}): centro {limites.center}, " +
               $"caja {caja.size}. Grupo de mixer: {(ambiente != null ? ambiente.name : "ninguno")}. " +
               $"Material de gotas: {(lluvia.materialGota != null ? RutaMaterial : "no se pudo crear")}. {guardado}";
    }

    // Renderiza una tabla de cada capa con los mismos generadores que usa el juego
    public static string ExportarTablas()
    {
        const int sr = 48000;
        Directory.CreateDirectory(Carpeta);
        var tablas = new (string nombre, float[] datos)[]
        {
            ("clima_lluvia_suave.wav",  SistemaLluvia.Generar(GeneradorLluvia.Parametros.Suave(),  sr, 5f, 1, 0.012f, 100)[0]),
            ("clima_lluvia_fuerte.wav", SistemaLluvia.Generar(GeneradorLluvia.Parametros.Fuerte(), sr, 5f, 1, 0.03f, 200)[0]),
            ("clima_viento_suave.wav",  ProceduralWind.Generar(GeneradorViento.Parametros.Suave(), sr, 10f, 1, 0.01f, 300)[0]),
            ("clima_viento_fuerte.wav", ProceduralWind.Generar(GeneradorViento.Parametros.Fuerte(), sr, 10f, 1, 0.028f, 400)[0]),
        };
        foreach (var (nombre, datos) in tablas)
            ExportarPasos.EscribirWav(Path.Combine(Carpeta, nombre), datos, 1, sr);
        return "Sonidos de clima exportados en " + Carpeta + ": " + string.Join(", ", tablas.Select(t => t.nombre));
    }

    static void ConfigurarAudio(GameObject objeto, AudioMixerGroup grupo, float variacionPitch, float variacionVolumen)
    {
        var fuente = Obtener<AudioSource>(objeto);
        Undo.RecordObject(fuente, "Área de Clima");
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f;
        fuente.outputAudioMixerGroup = grupo;
        EditorUtility.SetDirty(fuente);

        var reproductor = Obtener<ReproductorWavetable>(objeto);
        Undo.RecordObject(reproductor, "Área de Clima");
        reproductor.variacionPitch = variacionPitch;
        reproductor.variacionVolumen = variacionVolumen;
        EditorUtility.SetDirty(reproductor);
    }

    static bool LimitesParque(Scene escena, out Bounds limites, out string origen)
    {
        Bounds total = default;
        bool hay = false;
        GameObject parque = escena.GetRootGameObjects().FirstOrDefault(g => g.name == NombreParque);
        origen = parque != null ? NombreParque : "los terrenos de la escena";

        void Sumar(Bounds b)
        {
            if (!hay) { total = b; hay = true; }
            else total.Encapsulate(b);
        }

        if (parque != null)
            foreach (var r in parque.GetComponentsInChildren<Renderer>()) Sumar(r.bounds);

        var raices = parque != null ? new[] { parque } : escena.GetRootGameObjects();
        foreach (var raiz in raices)
            foreach (var t in raiz.GetComponentsInChildren<Terrain>())
                if (t.terrainData != null)
                    Sumar(new Bounds(t.GetPosition() + t.terrainData.size * 0.5f, t.terrainData.size));

        limites = total;
        return hay;
    }

    static Material MaterialGota()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) return null;

        if (!AssetDatabase.IsValidFolder(CarpetaMaterial))
            AssetDatabase.CreateFolder("Assets/Materials", "Clima");

        // Partícula transparente con mezcla alfa normal
        material = new Material(shader) { name = "GotaLluvia" };
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetColor("_BaseColor", new Color(0.8f, 0.85f, 0.95f, 0.45f));
        AssetDatabase.CreateAsset(material, RutaMaterial);
        return material;
    }

    static GameObject Hijo(GameObject padre, string nombre)
    {
        Transform t = padre.transform.Find(nombre);
        if (t != null) return t.gameObject;

        var hijo = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(hijo, "Área de Clima");
        hijo.transform.SetParent(padre.transform, false);
        return hijo;
    }

    static T Obtener<T>(GameObject objeto) where T : Component
    {
        // Comparación explícita: en el editor GetComponent puede devolver un "nulo falso" que ?? no detecta
        T componente = objeto.GetComponent<T>();
        return componente != null ? componente : Undo.AddComponent<T>(objeto);
    }
}
