// ScreenshotTool.cs  --  report evidence helper
// -----------------------------------------------------------------------------
// Renders the scene through the Main Camera and writes a PNG into Screenshots/.
// Used while building the scene to check how it looks without pressing Play
// without a human pressing Play, and by the student to collect report screenshots.
//
// Menu: Whack-a-Mole / Capture Screenshot
// -----------------------------------------------------------------------------

using System.IO;
using UnityEditor;
using UnityEngine;

namespace WhackAMole.EditorTools
{
    public static class ScreenshotTool
    {
        const string OutFolder = "Screenshots";
        const int Width = 1280;
        const int Height = 720;

        [MenuItem("Whack-a-Mole/Capture Screenshot", false, 40)]
        public static void Capture()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[ScreenshotTool] No camera tagged MainCamera in the open scene.");
                return;
            }

            // Render into an off-screen texture so no Game view window is needed.
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = cam.targetTexture;
            var previousActive = RenderTexture.active;

            cam.targetTexture = rt;

            // URP does not render through Camera.Render() in the Editor, so ask the
            // render pipeline itself for one frame into our texture.
            var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest();
            request.destination = rt;
            if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(cam, request))
            {
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam, request);
            }
            else
            {
                cam.Render(); // Built-in pipeline fallback
            }

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            if (!Directory.Exists(OutFolder)) Directory.CreateDirectory(OutFolder);

            string fileName = "scene_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            string path = Path.Combine(OutFolder, fileName);
            File.WriteAllBytes(path, tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log("[ScreenshotTool] Saved " + path);
        }
    }
}
