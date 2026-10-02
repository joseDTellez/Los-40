using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Revisa las escenas del build en busca de scripts faltantes, prefabs rotos y referencias "Missing".
// Desde Unity: menu Los 40 > Validar escenas del build.
// Desde terminal: Unity -batchmode -projectPath <ruta> -executeMethod ValidadorEscenas.ValidarBatch -quit
public static class ValidadorEscenas
{
    [MenuItem("Los 40/Validar escenas del build")]
    public static void ValidarDesdeMenu()
    {
        int problemas = ValidarEscenasDelBuild(new List<string>());
        EditorUtility.DisplayDialog("Validar escenas",
            problemas == 0 ? "Sin problemas." : problemas + " problema(s). Revisa la consola.", "OK");
    }

    public static void ValidarBatch()
    {
        int problemas = ValidarEscenasDelBuild(new List<string>());
        EditorApplication.Exit(problemas == 0 ? 0 : 1);
    }

    // No cierra ni descarta nada: las escenas ya abiertas se revisan tal como están en memoria
    // y las demás se abren de forma aditiva y se cierran al terminar.
    public static int ValidarEscenasDelBuild(List<string> mensajes)
    {
        int total = 0;

        foreach (var entrada in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            Scene escena = SceneManager.GetSceneByPath(entrada.path);
            bool abiertaAqui = !(escena.IsValid() && escena.isLoaded);
            if (abiertaAqui)
                escena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Additive);

            int problemas = ValidarEscena(escena, mensajes);
            total += problemas;
            Informar(mensajes, $"[Validador] {entrada.path}: {problemas} problema(s).", false);

            if (abiertaAqui)
                EditorSceneManager.CloseScene(escena, true);
        }

        Informar(mensajes, $"[Validador] Total: {total} problema(s).", false);
        return total;
    }

    public static int ValidarEscena(Scene escena, List<string> mensajes)
    {
        int problemas = 0;

        foreach (var raiz in escena.GetRootGameObjects())
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
        {
            GameObject go = t.gameObject;
            string ruta = Ruta(t);

            int faltantes = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (faltantes > 0)
            {
                Informar(mensajes, $"[Validador] {escena.name}: '{ruta}' tiene {faltantes} script(s) faltante(s).", true, go);
                problemas++;
            }

            if (PrefabUtility.IsPrefabAssetMissing(go))
            {
                Informar(mensajes, $"[Validador] {escena.name}: '{ruta}' es una instancia de prefab sin asset.", true, go);
                problemas++;
            }

            foreach (var componente in go.GetComponents<Component>())
            {
                if (componente == null) continue;

                var prop = new SerializedObject(componente).GetIterator();
                while (prop.Next(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.objectReferenceValue != null || prop.objectReferenceInstanceIDValue == 0) continue;

                    Informar(mensajes, $"[Validador] {escena.name}: '{ruta}' > {componente.GetType().Name}.{prop.propertyPath} apunta a un asset que ya no existe.", true, go);
                    problemas++;
                }
            }
        }

        return problemas;
    }

    public static string Ruta(Transform t)
    {
        return t.parent == null ? t.name : Ruta(t.parent) + "/" + t.name;
    }

    static void Informar(List<string> mensajes, string texto, bool esError, Object contexto = null)
    {
        mensajes.Add(texto);
        if (esError) Debug.LogError(texto, contexto);
        else Debug.Log(texto);
    }
}
