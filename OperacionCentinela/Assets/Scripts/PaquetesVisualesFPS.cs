// Integra los modelos CC0 de Quaternius y Kenney.
// Se distribuye junto a OperacionCentinelaFPS.cs y los assets en Resources.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class PaquetesVisualesFPS
{
    private static readonly Dictionary<string, Material> Materiales =
        new Dictionary<string, Material>();

    private static Shader ShaderJuego()
    {
        Shader s = GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit") : null;
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
        return s;
    }

    private static Material MaterialPara(string tipo, string origen)
    {
        string nombre = (origen ?? "").ToLowerInvariant();
        string clave = tipo + "|" + nombre;
        Material guardado;
        if (Materiales.TryGetValue(clave, out guardado) && guardado != null)
            return guardado;

        Color color = new Color(.52f, .61f, .69f);
        string textura = null;
        bool emision = false;
        if (tipo == "SciFiEnemigo")
        {
            textura = nombre.Contains("large") ?
                "ThirdParty/SciFi/Textures/T_Enemies_Large_BaseColor" :
                "ThirdParty/SciFi/Textures/T_Enemies_BaseColor";
        }
        else if (tipo == "SciFiArma")
            textura = nombre.Contains("batch2") ?
                "ThirdParty/SciFi/Textures/T_Guns_Batch2_BaseColor" :
                "ThirdParty/SciFi/Textures/T_Guns_Batch1_BaseColor";
        else if (tipo == "SciFiProp")
        {
            textura = nombre.Contains("crate") ?
                "ThirdParty/SciFi/Textures/T_Props_Crates_BaseColor" :
                nombre.Contains("batch2") ?
                "ThirdParty/SciFi/Textures/T_Props_Batch2_BaseColor" :
                "ThirdParty/SciFi/Textures/T_Props_Batch1_BaseColor";
        }
        else if (tipo == "Kenney")
        {
            textura = "ThirdParty/Kenney/Textures/colormap";
        }
        else if (tipo == "Mecha")
        {
            if (nombre.Contains("main")) color = new Color(.66f, .38f, .15f);
            else if (nombre.Contains("accent")) color = new Color(.90f, .16f, .08f);
            else if (nombre.Contains("eye"))
            {
                color = new Color(1f, .20f, .10f);
                emision = true;
            }
            else if (nombre.Contains("lightgrey")) color = new Color(.65f, .71f, .74f);
            else if (nombre.Contains("grey")) color = new Color(.31f, .40f, .47f);
            else color = new Color(.08f, .11f, .17f);
        }
        else if (tipo == "Modular")
        {
            if (nombre.Contains("light"))
            {
                color = new Color(.22f, .86f, 1f);
                emision = true;
            }
            else if (nombre.Contains("accent") && !nombre.Contains("dark"))
                color = new Color(.92f, .49f, .18f);
            else if (nombre.Contains("main")) color = new Color(.43f, .51f, .56f);
            else if (nombre.Contains("black")) color = new Color(.075f, .09f, .12f);
            else color = new Color(.18f, .24f, .29f);
        }
        Material m = new Material(ShaderJuego());
        m.name = "FPS_" + clave;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", textura != null ?
            Color.white : color);
        else if (m.HasProperty("_Color")) m.SetColor("_Color",
            textura != null ? Color.white : color);
        Texture2D tex = textura != null ? Resources.Load<Texture2D>(textura) : null;
        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            else if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        }
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", .40f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .38f);
        if (emision && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 2.4f);
        }
        Materiales[clave] = m;
        return m;
    }

    private static void Pintar(GameObject objeto, string tipo)
    {
        foreach (Renderer r in objeto.GetComponentsInChildren<Renderer>(true))
        {
            Material[] originales = r.sharedMaterials;
            if (originales.Length == 0) continue;
            Material[] nuevos = new Material[originales.Length];
            for (int i = 0; i < nuevos.Length; i++)
                nuevos[i] = MaterialPara(tipo,
                    originales[i] == null ? "" : originales[i].name);
            r.sharedMaterials = nuevos;
        }
    }

    private static bool CalcularBounds(GameObject o, out Bounds bounds)
    {
        Renderer[] renders = o.GetComponentsInChildren<Renderer>(true);
        if (renders.Length == 0)
        {
            bounds = new Bounds();
            return false;
        }
        bounds = renders[0].bounds;
        for (int i = 1; i < renders.Length; i++)
            bounds.Encapsulate(renders[i].bounds);
        return true;
    }

    public static GameObject Personaje(string ruta, Transform padre,
        float alturaFinal, float hover, string tipo, float giroY = 180f)
    {
        GameObject archivo = Resources.Load<GameObject>(ruta);
        if (archivo == null) return null;
        GameObject o = Object.Instantiate(archivo, padre, false);
        o.name = "Pack " + ruta.Substring(ruta.LastIndexOf('/') + 1);
        o.transform.localPosition = Vector3.zero;
        o.transform.localRotation = Quaternion.Euler(0, giroY, 0);
        o.transform.localScale = Vector3.one;

        Bounds b;
        if (!CalcularBounds(o, out b) || b.size.y < .01f)
        {
            Object.Destroy(o);
            return null;
        }
        float factor = alturaFinal / b.size.y;
        o.transform.localScale = Vector3.one * factor;
        CalcularBounds(o, out b);
        o.transform.position += new Vector3(
            padre.position.x - b.center.x,
            padre.position.y + hover - b.min.y,
            padre.position.z - b.center.z);
        Pintar(o, tipo);
        foreach (Collider c in o.GetComponentsInChildren<Collider>(true))
            Object.Destroy(c);
        // No dependemos del componente Animator; algunos proyectos
        // Unity 6 tienen deshabilitado el modulo integrado Animation.
        // Los FBX se representan correctamente con su pose inicial.
        return o;
    }

    public static GameObject Decoracion(string ruta, Transform padre,
        Vector3 posicion, float altura, string tipo, float giroY = 0f)
    {
        GameObject o = Personaje(ruta, padre, altura, 0, tipo, giroY);
        if (o == null) return null;
        o.transform.position += posicion;
        return o;
    }

    public static GameObject Arma(string ruta, Transform padre,
        Vector3 centroLocal, float longitud, string tipo,
        Vector3 rotacion)
    {
        GameObject archivo = Resources.Load<GameObject>(ruta);
        if (archivo == null) return null;
        GameObject o = Object.Instantiate(archivo, padre, false);
        o.name = "Arma importada - " + ruta;
        o.transform.localPosition = Vector3.zero;
        o.transform.localRotation = Quaternion.Euler(rotacion);
        o.transform.localScale = Vector3.one;
        Bounds b;
        if (!CalcularBounds(o, out b))
        {
            Object.Destroy(o);
            return null;
        }
        float d = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (d < .01f) d = 1f;
        o.transform.localScale = Vector3.one * longitud / d;
        CalcularBounds(o, out b);
        // Centramos los meshes importados alrededor del punto de agarre.
        Vector3 centroActual = padre.InverseTransformPoint(b.center);
        o.transform.localPosition += centroLocal - centroActual;
        Pintar(o, tipo);
        foreach (Collider c in o.GetComponentsInChildren<Collider>(true))
            Object.Destroy(c);
        return o;
    }
}
