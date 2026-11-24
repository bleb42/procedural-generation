using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonGen.Editor
{
    internal enum MapCorner
    {
        TopRight,
        TopLeft,
        BottomRight,
        BottomLeft
    }

    /// <summary>Creates the scene objects of the package: the builder and the map UI.</summary>
    internal static class SceneSetup
    {
        private const float MapMargin = 16f;

        public static DungeonBuilder CreateBuilder(DungeonProfile profile)
        {
            var go = new GameObject("Dungeon Builder");
            Undo.RegisterCreatedObjectUndo(go, "Create Dungeon Builder");

            var builder = go.AddComponent<DungeonBuilder>();

            if (profile != null)
                EditorAssets.SetReference(builder, "_profile", profile);

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);

            return builder;
        }

        public static MiniMapView CreateMiniMap(DungeonBuilder builder, Transform player, MapCorner corner, float size)
        {
            var canvas = EnsureCanvas();
            var panel = CreateUiObject("Mini Map", canvas.transform);

            var background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.raycastTarget = false;

            PlaceInCorner((RectTransform)panel.transform, corner, size);

            var content = CreateUiObject("Content", panel.transform);
            var marker = CreateMarker(panel.transform);
            var view = panel.AddComponent<MiniMapView>();

            EditorAssets.SetReference(view, "_content", content.transform);
            EditorAssets.SetReference(view, "_playerMarker", marker.transform);

            LinkPanel(builder, player, view, null);
            Finish(panel);

            return view;
        }

        public static FullMapView CreateFullMap(DungeonBuilder builder, Transform player)
        {
            var canvas = EnsureCanvas();
            var panel = CreateUiObject("Full Map", canvas.transform);

            var background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.8f);

            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(900f, 600f);

            var content = CreateUiObject("Content", panel.transform);
            var marker = CreateMarker(panel.transform);
            var view = panel.AddComponent<FullMapView>();

            EditorAssets.SetReference(view, "_content", content.transform);
            EditorAssets.SetReference(view, "_playerMarker", marker.transform);

            LinkPanel(builder, player, null, view);

            // Hidden by default. Show it from your own code, for example on a key press.
            panel.SetActive(false);
            Finish(panel);

            return view;
        }

        private static void LinkPanel(DungeonBuilder builder, Transform player, MiniMapView mini, FullMapView full)
        {
            var mapPanel = UnityEngine.Object.FindFirstObjectByType<MapPanel>();

            if (mapPanel == null)
            {
                var go = new GameObject("Map Panel");
                Undo.RegisterCreatedObjectUndo(go, "Create Map Panel");
                mapPanel = go.AddComponent<MapPanel>();
            }

            if (mini != null)
                EditorAssets.SetReference(mapPanel, "_mini", mini);

            if (full != null)
                EditorAssets.SetReference(mapPanel, "_full", full);

            if (builder != null)
                EditorAssets.SetReference(mapPanel, "_builder", builder);

            if (player != null)
                EditorAssets.SetReference(mapPanel, "_player", player);
        }

        private static Canvas EnsureCanvas()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<Canvas>();

            if (existing != null)
            {
                EnsureEventSystem();
                return existing.rootCanvas;
            }

            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            EnsureEventSystem();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");

            // The new Input System needs its own module. It is looked up by name so the package does not depend on it.
            var inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            go.AddComponent(inputSystemModule ?? typeof(StandaloneInputModule));
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            return go;
        }

        private static GameObject CreateMarker(Transform parent)
        {
            var marker = CreateUiObject("Player Marker", parent);

            var image = marker.AddComponent<Image>();
            image.color = DungeonGenStyles.Accent;
            image.raycastTarget = false;

            ((RectTransform)marker.transform).sizeDelta = new Vector2(10f, 14f);

            return marker;
        }

        private static void PlaceInCorner(RectTransform rect, MapCorner corner, float size)
        {
            bool top = corner == MapCorner.TopRight || corner == MapCorner.TopLeft;
            bool right = corner == MapCorner.TopRight || corner == MapCorner.BottomRight;

            var anchor = new Vector2(right ? 1f : 0f, top ? 1f : 0f);

            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = new Vector2(right ? -MapMargin : MapMargin, top ? -MapMargin : MapMargin);
            rect.sizeDelta = new Vector2(size, size);
        }

        private static void Finish(GameObject created)
        {
            Selection.activeGameObject = created;
            EditorSceneManager.MarkSceneDirty(created.scene);
        }
    }
}
