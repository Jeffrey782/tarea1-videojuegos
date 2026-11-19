// Herramienta opcional de recuperacion de la escena para Unity Editor.
// No se incluye en la compilacion del juego.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrepararEscenaFPS
{
    [MenuItem("Operacion Centinela/Recrear escena principal")]
    public static void Recrear()
    {
        const string ruta = "Assets/Scenes/01 Galeria de disparos.unity";
        if (!EditorUtility.DisplayDialog("Operacion Centinela",
            "Esto reemplazara la escena 01 Galeria de disparos del proyecto. " +
            "¿Deseas recrearla?", "Si, recrear", "Cancelar"))
            return;

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        GameObject director = new GameObject("Director del juego");
        director.AddComponent<OperacionCentinelaFPS>();
        EditorSceneManager.SaveScene(escena, ruta);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene(ruta, true)
        };
        Selection.activeGameObject = director;
        Debug.Log("Escena preparada: " + ruta);
    }
}
#endif
