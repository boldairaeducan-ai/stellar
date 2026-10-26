using StellarIdle.UI;
using UnityEngine;

namespace StellarIdle
{
    /// <summary>
    /// Starts the game in whatever scene is loaded first: press Play from any scene, even an empty one.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;

            EnsureCamera();
            var game = new GameObject("GameManager").AddComponent<GameManager>();
            MainScreen.Create(game);
        }

        static void EnsureCamera()
        {
            if (Object.FindAnyObjectByType<Camera>() != null) return;
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Background;
        }
    }
}
