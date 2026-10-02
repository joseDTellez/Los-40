using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Puente entre Claude Code y el editor ABIERTO, para aplicar cambios sin cerrar Unity.
//
// Claude escribe ComandosClaude/comando.json (carpeta junto a Assets, fuera de git). El editor lo
// detecta, importa lo que haya cambiado en disco, aplica las operaciones con la API de Unity
// (con Undo) y deja el resultado en ComandosClaude/resultado.json. Además, cada compilación deja
// sus errores en ComandosClaude/compilacion.json.
//
// Con Unity cerrado se aplica el mismo archivo con:
//   Unity -batchmode -projectPath <ruta> -executeMethod PuenteClaude.EjecutarPendienteBatch -quit
//
// Operaciones (campo "tipo"):
//   listar             escena|prefab, [objeto = prefijo de ruta], [valor = texto a buscar]
//   leer               escena|prefab, objeto, componente, [propiedad]
//   asignar            escena|prefab, objeto, componente, propiedad, valor
//   asignarTodos       escena|prefab, componente, propiedad, valor, [objeto = prefijo de ruta],
//                      [filtro = solo si el valor actual contiene este texto]
//   agregarComponente  escena|prefab, objeto, componente
//   quitarComponente   escena|prefab, objeto, componente
//   crearObjeto        escena|prefab, [objeto = ruta del padre], valor = nombre
//   validar            revisa las escenas del build (scripts faltantes, referencias rotas)
//   ejecutar           metodo = "Clase.Metodo" estático y sin parámetros
//
// "componente" puede ser "GameObject" para tocar el propio objeto (m_Name, m_IsActive, m_Layer...).
// Para referencias, "valor" es la ruta de un asset ("Assets/.../x.wav"), "ruta::Nombre" para un
// sub-asset (p. ej. "Assets/MainAudioMixer.mixer::Musica"), "escena:Ruta/Objeto|Componente" para
// un objeto de la misma escena/prefab, o "null". Los arrays se redimensionan con "lista.Array.size"
// y sus elementos se tocan con "lista.Array.data[0].campo".
[InitializeOnLoad]
public static class PuenteClaude
{
    const string PrefActivo = "Los40.PuenteClaude.Activo";
    const string MenuActivo = "Los 40/Puente Claude/Aplicar cambios automáticamente";
    const int MaxLineas = 400;

    static string Carpeta => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ComandosClaude"));
    static string RutaComando => Path.Combine(Carpeta, "comando.json");
    static string RutaResultado => Path.Combine(Carpeta, "resultado.json");
    static string RutaCompilacion => Path.Combine(Carpeta, "compilacion.json");
    static string RutaHistorial => Path.Combine(Carpeta, "historial.log");

    static double siguienteRevision;
    static readonly List<string> erroresCompilacion = new List<string>();
    static int advertenciasCompilacion;

    static bool Activo
    {
        get => EditorPrefs.GetBool(PrefActivo, true);
        set => EditorPrefs.SetBool(PrefActivo, value);
    }

    static PuenteClaude()
    {
        if (Application.isBatchMode) return;

        EditorApplication.update += Revisar;
        CompilationPipeline.compilationStarted += _ => { erroresCompilacion.Clear(); advertenciasCompilacion = 0; };
        CompilationPipeline.assemblyCompilationFinished += RegistrarMensajes;
        CompilationPipeline.compilationFinished += _ => GuardarEstadoCompilacion();
    }

    // ─── MENÚ ─────────────────────────────

    [MenuItem(MenuActivo)]
    static void AlternarActivo() => Activo = !Activo;

    [MenuItem(MenuActivo, true)]
    static bool AlternarActivoValidar()
    {
        Menu.SetChecked(MenuActivo, Activo);
        return true;
    }

    [MenuItem("Los 40/Puente Claude/Aplicar comando pendiente ahora")]
    public static void AplicarDesdeMenu()
    {
        AssetDatabase.Refresh();
        if (EditorApplication.isCompiling)
        {
            Debug.Log("[Puente Claude] Compilando scripts; el comando se aplicará al terminar.");
            return;
        }
        EjecutarPendiente();
    }

    // ─── DETECCIÓN AUTOMÁTICA ─────────────────────────────

    static void Revisar()
    {
        if (EditorApplication.timeSinceStartup < siguienteRevision) return;
        siguienteRevision = EditorApplication.timeSinceStartup + 1.0;

        if (!Activo || !File.Exists(RutaComando)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Primero importamos lo que haya cambiado en disco (scripts, audios...). Si eso provoca
        // una recompilación, el comando se aplica en la siguiente revisión, ya con el código nuevo.
        AssetDatabase.Refresh();
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        EjecutarPendiente();
    }

    public static void EjecutarPendienteBatch()
    {
        AssetDatabase.Refresh();
        Resultado r = EjecutarPendiente();
        EditorApplication.Exit(r != null && r.ok ? 0 : 1);
    }

    static Resultado EjecutarPendiente()
    {
        if (!File.Exists(RutaComando))
        {
            Debug.Log("[Puente Claude] No hay comando pendiente.");
            return null;
        }

        string json = File.ReadAllText(RutaComando);
        File.Delete(RutaComando);

        Resultado r = Procesar(json);
        File.WriteAllText(RutaResultado, JsonUtility.ToJson(r, true));
        File.AppendAllText(RutaHistorial, $"{r.hora} [{r.id}] {(r.ok ? "OK" : "ERROR")}\n  " + string.Join("\n  ", r.mensajes) + "\n");

        string resumen = $"[Puente Claude] Comando '{r.id}': {(r.ok ? "aplicado" : "con errores")}.\n" + string.Join("\n", r.mensajes);
        if (r.ok) Debug.Log(resumen); else Debug.LogWarning(resumen);
        return r;
    }

    // ─── COMPILACIÓN ─────────────────────────────

    static void RegistrarMensajes(string ensamblado, CompilerMessage[] mensajes)
    {
        foreach (var m in mensajes)
        {
            if (m.type == CompilerMessageType.Error) erroresCompilacion.Add(m.message);
            else if (m.type == CompilerMessageType.Warning) advertenciasCompilacion++;
        }
    }

    static void GuardarEstadoCompilacion()
    {
        var estado = new EstadoCompilacion
        {
            hora = DateTime.Now.ToString("s"),
            ok = erroresCompilacion.Count == 0,
            advertencias = advertenciasCompilacion,
            errores = new List<string>(erroresCompilacion)
        };
        Directory.CreateDirectory(Carpeta);
        File.WriteAllText(RutaCompilacion, JsonUtility.ToJson(estado, true));
    }

    // ─── PROCESAMIENTO ─────────────────────────────

    static Resultado Procesar(string json)
    {
        var r = new Resultado { hora = DateTime.Now.ToString("s"), compilacionConErrores = EditorUtility.scriptCompilationFailed };

        Comando cmd;
        try { cmd = JsonUtility.FromJson<Comando>(json); }
        catch (Exception e)
        {
            r.ok = false;
            r.mensajes.Add("JSON inválido: " + e.Message);
            return r;
        }

        r.id = cmd.id;
        var sesion = new Sesion();
        try
        {
            foreach (var op in cmd.ops)
            {
                try { Ejecutar(op, sesion, r); }
                catch (Exception e)
                {
                    r.ok = false;
                    r.mensajes.Add($"[{op.tipo}] ERROR: {e.Message}");
                }
            }
        }
        finally
        {
            sesion.Cerrar(r);
        }
        return r;
    }

    static void Ejecutar(Operacion op, Sesion sesion, Resultado r)
    {
        switch (op.tipo)
        {
            case "validar":
                ValidadorEscenas.ValidarEscenasDelBuild(r.mensajes);
                return;

            case "ejecutar":
                EjecutarMetodo(op.metodo, r);
                return;

            case "listar":
                Listar(sesion.Raices(op), op.objeto, op.valor, r);
                return;

            case "asignarTodos":
                AsignarTodos(op, sesion, r);
                return;
        }

        if (op.tipo == "crearObjeto")
        {
            CrearObjeto(op, sesion, r);
            return;
        }

        GameObject go = sesion.BuscarObjeto(op, r);

        switch (op.tipo)
        {
            case "leer":
                Leer(go, op, r);
                break;

            case "asignar":
            {
                Object destino = BuscarComponente(go, op.componente);
                var so = new SerializedObject(destino);
                SerializedProperty prop = so.FindProperty(op.propiedad)
                    ?? throw new Exception($"'{op.componente}' no tiene la propiedad '{op.propiedad}'.");

                string antes = Describir(prop);
                AsignarValor(prop, op.valor, op, sesion);
                if (!so.ApplyModifiedProperties())
                {
                    r.mensajes.Add($"{op.objeto} > {op.componente}.{op.propiedad}: sin cambios ({antes}).");
                    break;
                }
                sesion.MarcarModificado(op);
                r.mensajes.Add($"{op.objeto} > {op.componente}.{op.propiedad}: {antes} -> {Describir(so.FindProperty(op.propiedad))}");
                break;
            }

            case "agregarComponente":
            {
                Type tipo = BuscarTipo(op.componente);
                if (go.GetComponent(tipo) != null)
                {
                    r.mensajes.Add($"{op.objeto}: ya tiene {tipo.Name}.");
                    break;
                }
                Undo.AddComponent(go, tipo);
                sesion.MarcarModificado(op);
                r.mensajes.Add($"{op.objeto}: agregado {tipo.Name}.");
                break;
            }

            case "quitarComponente":
            {
                Component c = BuscarComponente(go, op.componente) as Component
                    ?? throw new Exception("No se puede quitar el GameObject con quitarComponente.");
                Undo.DestroyObjectImmediate(c);
                sesion.MarcarModificado(op);
                r.mensajes.Add($"{op.objeto}: quitado {op.componente}.");
                break;
            }

            default:
                throw new Exception($"Operación desconocida '{op.tipo}'.");
        }
    }

    // ─── OPERACIONES ─────────────────────────────

    static void CrearObjeto(Operacion op, Sesion sesion, Resultado r)
    {
        if (string.IsNullOrEmpty(op.valor)) throw new Exception("crearObjeto necesita 'valor' con el nombre.");

        GameObject[] raices = sesion.Raices(op); // también resuelve la escena activa si no se indicó
        Transform padre = !string.IsNullOrEmpty(op.objeto) ? sesion.BuscarObjeto(op, r).transform
                        : !string.IsNullOrEmpty(op.prefab) ? raices[0].transform
                        : null;

        var nuevo = new GameObject(op.valor);
        Undo.RegisterCreatedObjectUndo(nuevo, "Puente Claude: crear " + op.valor);

        if (padre != null)
            nuevo.transform.SetParent(padre, false);
        else
            SceneManager.MoveGameObjectToScene(nuevo, sesion.Escena(op.escena));

        sesion.MarcarModificado(op);
        r.mensajes.Add($"Creado '{ValidadorEscenas.Ruta(nuevo.transform)}'.");
    }

    static void AsignarTodos(Operacion op, Sesion sesion, Resultado r)
    {
        Type tipo = BuscarTipo(op.componente);
        int revisados = 0, cambiados = 0;

        foreach (var raiz in sesion.Raices(op))
        foreach (Component c in raiz.GetComponentsInChildren(tipo, true))
        {
            if (!string.IsNullOrEmpty(op.objeto) && !ValidadorEscenas.Ruta(c.transform).StartsWith(op.objeto)) continue;

            var so = new SerializedObject(c);
            SerializedProperty prop = so.FindProperty(op.propiedad)
                ?? throw new Exception($"'{tipo.Name}' no tiene la propiedad '{op.propiedad}'.");
            if (!string.IsNullOrEmpty(op.filtro) && Describir(prop).IndexOf(op.filtro, StringComparison.OrdinalIgnoreCase) < 0) continue;

            revisados++;
            AsignarValor(prop, op.valor, op, sesion);
            if (so.ApplyModifiedProperties()) cambiados++;
        }

        if (cambiados > 0) sesion.MarcarModificado(op);
        r.mensajes.Add($"{tipo.Name}.{op.propiedad} = {op.valor}: {cambiados} cambiado(s) de {revisados} que cumplían el filtro.");
    }

    static void Listar(GameObject[] raices, string prefijo, string filtro, Resultado r)
    {
        int lineas = 0, total = 0;
        foreach (var raiz in raices)
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
        {
            string ruta = ValidadorEscenas.Ruta(t);
            if (!string.IsNullOrEmpty(prefijo) && !ruta.StartsWith(prefijo)) continue;
            if (!string.IsNullOrEmpty(filtro) && ruta.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) < 0) continue;

            total++;
            if (lineas >= MaxLineas) continue;
            string componentes = string.Join(", ", t.GetComponents<Component>().Select(c => c == null ? "<script faltante>" : c.GetType().Name));
            r.mensajes.Add($"{ruta}{(t.gameObject.activeSelf ? "" : " (inactivo)")} [{componentes}]");
            lineas++;
        }
        if (total > lineas) r.mensajes.Add($"... {total - lineas} objeto(s) más; usa 'objeto' o 'valor' para filtrar.");
    }

    static void Leer(GameObject go, Operacion op, Resultado r)
    {
        var so = new SerializedObject(BuscarComponente(go, op.componente));

        if (!string.IsNullOrEmpty(op.propiedad))
        {
            SerializedProperty p = so.FindProperty(op.propiedad)
                ?? throw new Exception($"'{op.componente}' no tiene la propiedad '{op.propiedad}'.");
            r.mensajes.Add($"{op.propiedad} = {Describir(p)}");
            return;
        }

        SerializedProperty it = so.GetIterator();
        int lineas = 0;
        bool entrar = true;
        while (it.NextVisible(entrar) && lineas < MaxLineas)
        {
            entrar = it.propertyType == SerializedPropertyType.Generic && it.depth < 2 && !it.isArray;
            r.mensajes.Add($"{new string(' ', it.depth * 2)}{it.propertyPath} = {Describir(it)}");
            lineas++;
        }
    }

    static void EjecutarMetodo(string metodo, Resultado r)
    {
        int punto = metodo?.LastIndexOf('.') ?? -1;
        if (punto <= 0) throw new Exception("'metodo' debe tener la forma Clase.Metodo.");

        string clase = metodo.Substring(0, punto), nombre = metodo.Substring(punto + 1);
        Type tipo = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(clase, false))
            .FirstOrDefault(t => t != null)
            ?? throw new Exception($"No existe la clase '{clase}'.");

        MethodInfo mi = tipo.GetMethod(nombre, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null)
            ?? throw new Exception($"'{clase}' no tiene un método estático '{nombre}()' sin parámetros.");

        object salida = mi.Invoke(null, null);
        if (salida is string s) r.mensajes.Add(s);
        else if (salida is IEnumerable lista) r.mensajes.AddRange(lista.Cast<object>().Select(x => x?.ToString()));
        r.mensajes.Add($"Ejecutado {metodo}().");
    }

    // ─── UTILIDADES ─────────────────────────────

    static Type BuscarTipo(string nombre)
    {
        if (nombre == "GameObject") return typeof(GameObject);
        var candidatos = TypeCache.GetTypesDerivedFrom<Component>().Where(t => t.Name == nombre || t.FullName == nombre).ToList();
        if (candidatos.Count == 0) throw new Exception($"No existe el componente '{nombre}'.");
        return candidatos[0];
    }

    static Object BuscarComponente(GameObject go, string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) throw new Exception("Falta 'componente'.");
        if (nombre == "GameObject") return go;

        Type tipo = BuscarTipo(nombre);
        return go.GetComponent(tipo)
            ?? throw new Exception($"'{go.name}' no tiene {nombre}. Tiene: {string.Join(", ", go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name))}.");
    }

    static void AsignarValor(SerializedProperty prop, string valor, Operacion op, Sesion sesion)
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.ArraySize:
                prop.intValue = int.Parse(valor, ci); break;
            case SerializedPropertyType.Float:
                prop.floatValue = float.Parse(valor, ci); break;
            case SerializedPropertyType.Boolean:
                prop.boolValue = valor == "1" || valor.Equals("true", StringComparison.OrdinalIgnoreCase); break;
            case SerializedPropertyType.String:
                prop.stringValue = valor; break;
            case SerializedPropertyType.Enum:
            {
                int i = Array.IndexOf(prop.enumNames, valor);
                prop.enumValueIndex = i >= 0 ? i : int.Parse(valor, ci);
                break;
            }
            case SerializedPropertyType.Color:
            {
                if (!ColorUtility.TryParseHtmlString(valor, out Color c))
                {
                    float[] v = Numeros(valor, 3);
                    c = new Color(v[0], v[1], v[2], v.Length > 3 ? v[3] : 1f);
                }
                prop.colorValue = c;
                break;
            }
            case SerializedPropertyType.Vector2:
            {
                float[] v = Numeros(valor, 2); prop.vector2Value = new Vector2(v[0], v[1]); break;
            }
            case SerializedPropertyType.Vector3:
            {
                float[] v = Numeros(valor, 3); prop.vector3Value = new Vector3(v[0], v[1], v[2]); break;
            }
            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = ResolverReferencia(prop, valor, op, sesion);
                break;
            default:
                throw new Exception($"No sé asignar propiedades de tipo {prop.propertyType}.");
        }
    }

    static float[] Numeros(string valor, int minimo)
    {
        float[] v = valor.Split(',').Select(x => float.Parse(x.Trim(), CultureInfo.InvariantCulture)).ToArray();
        if (v.Length < minimo) throw new Exception($"Se esperaban al menos {minimo} números separados por comas.");
        return v;
    }

    static Object ResolverReferencia(SerializedProperty prop, string valor, Operacion op, Sesion sesion)
    {
        if (string.IsNullOrEmpty(valor) || valor == "null") return null;

        if (valor.StartsWith("escena:"))
        {
            string[] partes = valor.Substring("escena:".Length).Split('|');
            var sub = new Operacion { escena = op.escena, prefab = op.prefab, objeto = partes[0] };
            GameObject go = sesion.BuscarObjeto(sub, null);
            return partes.Length > 1 ? BuscarComponente(go, partes[1]) : go;
        }

        // "ruta::Nombre" elige un sub-asset concreto (un grupo del mixer, un sprite de un atlas...)
        string subAsset = null;
        int separador = valor.IndexOf("::", StringComparison.Ordinal);
        if (separador >= 0)
        {
            subAsset = valor.Substring(separador + 2);
            valor = valor.Substring(0, separador);
        }

        // Asset: el principal o, si no encaja con el tipo del campo, un sub-asset (p. ej. un Sprite)
        Object[] candidatos = new[] { AssetDatabase.LoadMainAssetAtPath(valor) }
            .Concat(AssetDatabase.LoadAllAssetsAtPath(valor)).Where(o => o != null).ToArray();
        if (subAsset != null) candidatos = candidatos.Where(o => o.name == subAsset).ToArray();
        if (candidatos.Length == 0) throw new Exception($"No existe el asset '{valor}'{(subAsset != null ? " con el sub-asset '" + subAsset + "'" : "")}.");

        string tipoCampo = prop.type.StartsWith("PPtr<$") ? prop.type.Substring(6, prop.type.Length - 7) : null;
        Object elegido = candidatos.FirstOrDefault(o => tipoCampo == null || o.GetType().Name == tipoCampo)
                         ?? candidatos.FirstOrDefault(o => EsAsignable(o.GetType(), tipoCampo))
                         ?? throw new Exception($"'{valor}' no contiene un {tipoCampo}.");
        return elegido;
    }

    static bool EsAsignable(Type tipo, string nombreBase)
    {
        for (Type t = tipo; t != null; t = t.BaseType)
            if (t.Name == nombreBase) return true;
        return false;
    }

    static string Describir(SerializedProperty p)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer: return p.intValue.ToString();
            case SerializedPropertyType.LayerMask: return p.intValue.ToString();
            case SerializedPropertyType.ArraySize: return p.intValue.ToString();
            case SerializedPropertyType.Float: return p.floatValue.ToString(CultureInfo.InvariantCulture);
            case SerializedPropertyType.Boolean: return p.boolValue ? "true" : "false";
            case SerializedPropertyType.String: return "\"" + p.stringValue + "\"";
            case SerializedPropertyType.Enum: return p.enumValueIndex >= 0 && p.enumValueIndex < p.enumNames.Length ? p.enumNames[p.enumValueIndex] : p.intValue.ToString();
            case SerializedPropertyType.Color: return "#" + ColorUtility.ToHtmlStringRGBA(p.colorValue);
            case SerializedPropertyType.Vector2: return p.vector2Value.ToString();
            case SerializedPropertyType.Vector3: return p.vector3Value.ToString();
            case SerializedPropertyType.ObjectReference:
                if (p.objectReferenceValue == null)
                    return p.objectReferenceInstanceIDValue != 0 ? "<Missing>" : "null";
                string ruta = AssetDatabase.GetAssetPath(p.objectReferenceValue);
                if (string.IsNullOrEmpty(ruta) || p.objectReferenceValue is Component || p.objectReferenceValue is GameObject && !EditorUtility.IsPersistent(p.objectReferenceValue))
                    return $"{p.objectReferenceValue.name} ({p.objectReferenceValue.GetType().Name})";
                return AssetDatabase.IsSubAsset(p.objectReferenceValue) ? ruta + "::" + p.objectReferenceValue.name : ruta;
            default:
                return p.isArray ? $"[{p.arraySize} elementos]" : $"<{p.propertyType}>";
        }
    }

    // ─── ESCENAS Y PREFABS ─────────────────────────────

    // Abre lo necesario sin tocar el trabajo del usuario: las escenas que ya estaban abiertas se
    // modifican en memoria y solo se guardan si no tenían cambios previos sin guardar.
    class Sesion
    {
        class InfoEscena { public Scene escena; public bool abiertaAqui, suciaAntes, modificada; }
        class InfoPrefab { public GameObject raiz; public bool modificado; }

        readonly Dictionary<string, InfoEscena> escenas = new Dictionary<string, InfoEscena>();
        readonly Dictionary<string, InfoPrefab> prefabs = new Dictionary<string, InfoPrefab>();

        public Scene Escena(string nombreORuta)
        {
            string ruta = ResolverEscena(nombreORuta);
            if (escenas.TryGetValue(ruta, out var info)) return info.escena;

            Scene s = SceneManager.GetSceneByPath(ruta);
            bool abierta = s.IsValid() && s.isLoaded;
            if (!abierta) s = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Additive);
            escenas[ruta] = new InfoEscena { escena = s, abiertaAqui = !abierta, suciaAntes = abierta && s.isDirty };
            return s;
        }

        public GameObject[] Raices(Operacion op)
        {
            if (!string.IsNullOrEmpty(op.prefab))
            {
                if (!prefabs.TryGetValue(op.prefab, out var info))
                {
                    if (AssetDatabase.LoadMainAssetAtPath(op.prefab) == null) throw new Exception($"No existe el prefab '{op.prefab}'.");
                    info = new InfoPrefab { raiz = PrefabUtility.LoadPrefabContents(op.prefab) };
                    prefabs[op.prefab] = info;
                }
                return new[] { info.raiz };
            }

            if (string.IsNullOrEmpty(op.escena))
            {
                Scene activa = SceneManager.GetActiveScene();
                op.escena = activa.path;
            }
            return Escena(op.escena).GetRootGameObjects();
        }

        public GameObject BuscarObjeto(Operacion op, Resultado r)
        {
            if (string.IsNullOrEmpty(op.objeto)) throw new Exception("Falta 'objeto' (ruta desde la raíz, p. ej. Player/AudioPasos).");

            var coincidencias = new List<Transform>();
            foreach (var raiz in Raices(op))
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                if (ValidadorEscenas.Ruta(t) == op.objeto) coincidencias.Add(t);

            if (coincidencias.Count == 0)
            {
                string nombre = op.objeto.Split('/').Last();
                var parecidos = Raices(op).SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                    .Where(t => t.name.IndexOf(nombre, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Take(5).Select(t => ValidadorEscenas.Ruta(t));
                throw new Exception($"No existe '{op.objeto}'. Parecidos: {string.Join(" | ", parecidos)}");
            }
            if (op.indice >= coincidencias.Count)
                throw new Exception($"'{op.objeto}' tiene {coincidencias.Count} coincidencias; indice {op.indice} no existe.");
            if (coincidencias.Count > 1 && r != null)
                r.mensajes.Add($"Aviso: hay {coincidencias.Count} objetos '{op.objeto}'; se usa el indice {op.indice}.");

            return coincidencias[op.indice].gameObject;
        }

        public void MarcarModificado(Operacion op)
        {
            if (!string.IsNullOrEmpty(op.prefab))
            {
                prefabs[op.prefab].modificado = true;
                return;
            }
            var info = escenas[ResolverEscena(op.escena)];
            info.modificada = true;
            EditorSceneManager.MarkSceneDirty(info.escena);
        }

        public void Cerrar(Resultado r)
        {
            foreach (var par in prefabs)
            {
                if (par.Value.modificado)
                {
                    PrefabUtility.SaveAsPrefabAsset(par.Value.raiz, par.Key);
                    r.mensajes.Add($"Prefab guardado: {par.Key}");
                }
                PrefabUtility.UnloadPrefabContents(par.Value.raiz);
            }

            foreach (var info in escenas.Values)
            {
                if (info.modificada)
                {
                    if (info.suciaAntes)
                    {
                        r.mensajes.Add($"{info.escena.name}: tenía cambios tuyos sin guardar; apliqué los cambios pero NO guardé (guarda con Cmd+S).");
                    }
                    else
                    {
                        EditorSceneManager.SaveScene(info.escena);
                        r.mensajes.Add($"Escena guardada: {info.escena.path}");
                    }
                }
                if (info.abiertaAqui)
                    EditorSceneManager.CloseScene(info.escena, true);
            }
        }

        static string ResolverEscena(string nombreORuta)
        {
            if (string.IsNullOrEmpty(nombreORuta)) throw new Exception("Falta 'escena' o 'prefab'.");
            if (nombreORuta.EndsWith(".unity")) return nombreORuta;

            var rutas = AssetDatabase.FindAssets("t:Scene " + nombreORuta)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == nombreORuta).ToList();
            if (rutas.Count != 1) throw new Exception($"No encuentro una única escena '{nombreORuta}' ({rutas.Count} coincidencias).");
            return rutas[0];
        }
    }

    // ─── FORMATO DE LOS ARCHIVOS ─────────────────────────────

    [Serializable] class Comando { public string id; public List<Operacion> ops = new List<Operacion>(); }

    [Serializable]
    class Operacion
    {
        public string tipo, escena, prefab, objeto, componente, propiedad, valor, metodo, filtro;
        public int indice;
    }

    [Serializable]
    class Resultado
    {
        public string id, hora;
        public bool ok = true;
        public bool compilacionConErrores;
        public List<string> mensajes = new List<string>();
    }

    [Serializable]
    class EstadoCompilacion
    {
        public string hora;
        public bool ok;
        public int advertencias;
        public List<string> errores = new List<string>();
    }
}
