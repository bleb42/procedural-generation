using UnityEditor;
using UnityEngine;

namespace DungeonGen.Editor
{
    internal enum ButtonKind
    {
        Primary,
        Secondary,
        Danger
    }

    /// <summary>Colors, rounded textures and GUIStyles of the Dungeon Gen window.</summary>
    internal static class DungeonGenStyles
    {
        public static readonly Color Accent = new Color(0.30f, 0.80f, 0.62f);
        public static readonly Color Danger = new Color(0.86f, 0.36f, 0.36f);
        public static readonly Color Warning = new Color(0.95f, 0.72f, 0.30f);
        public static readonly Color Muted = new Color(0.45f, 0.48f, 0.52f);

        public static Color HeaderColor { get; private set; }
        public static Color TabBarColor { get; private set; }

        public static GUIStyle Title { get; private set; }
        public static GUIStyle Subtitle { get; private set; }
        public static GUIStyle Tab { get; private set; }
        public static GUIStyle TabActive { get; private set; }
        public static GUIStyle Card { get; private set; }
        public static GUIStyle CardTitle { get; private set; }
        public static GUIStyle Hint { get; private set; }
        public static GUIStyle StatValue { get; private set; }
        public static GUIStyle StatLabel { get; private set; }
        public static GUIStyle Row { get; private set; }

        private static GUIStyle _button;
        private static GUIStyle _pill;
        private static Texture2D _cardTexture;
        private static bool _builtForProSkin;

        public static void Ensure()
        {
            if (Card != null && _cardTexture != null && _builtForProSkin == EditorGUIUtility.isProSkin)
                return;

            Build();
        }

        /// <summary>A rounded button tinted by its kind. Returns true when clicked.</summary>
        public static bool Button(string text, ButtonKind kind, params GUILayoutOption[] options)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = ColorOf(kind);

            bool clicked = GUILayout.Button(text, _button, options);

            GUI.backgroundColor = previous;
            return clicked;
        }

        /// <summary>A small rounded label in the given color, for statuses.</summary>
        public static void Pill(string text, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;

            GUILayout.Label(text, _pill, GUILayout.Height(18f));

            GUI.color = previous;
        }

        public static void BeginCard(string title)
        {
            EditorGUILayout.BeginVertical(Card);
            GUILayout.Label(title, CardTitle);
            GUILayout.Space(4f);
        }

        public static void EndCard()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
        }

        private static Color ColorOf(ButtonKind kind)
        {
            switch (kind)
            {
                case ButtonKind.Primary:
                    return new Color(0.22f, 0.62f, 0.48f);

                case ButtonKind.Danger:
                    return new Color(0.70f, 0.30f, 0.30f);

                default:
                    return EditorGUIUtility.isProSkin ? new Color(0.36f, 0.38f, 0.42f) : new Color(0.62f, 0.64f, 0.68f);
            }
        }

        private static void Build()
        {
            bool pro = EditorGUIUtility.isProSkin;
            _builtForProSkin = pro;

            HeaderColor = pro ? new Color(0.09f, 0.11f, 0.13f) : new Color(0.16f, 0.20f, 0.24f);
            TabBarColor = pro ? new Color(0.14f, 0.16f, 0.18f) : new Color(0.80f, 0.82f, 0.85f);

            var textColor = pro ? new Color(0.86f, 0.88f, 0.90f) : new Color(0.12f, 0.14f, 0.16f);
            var mutedText = pro ? new Color(0.62f, 0.65f, 0.69f) : new Color(0.35f, 0.38f, 0.42f);
            var cardColor = pro ? new Color(0.20f, 0.22f, 0.24f) : new Color(0.90f, 0.91f, 0.93f);

            _cardTexture = Rounded(cardColor);

            Title = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            Subtitle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.65f, 0.72f, 0.78f) }
            };

            Tab = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = mutedText },
                hover = { textColor = textColor }
            };

            TabActive = new GUIStyle(Tab)
            {
                normal = { textColor = textColor },
                hover = { textColor = textColor }
            };

            Card = new GUIStyle
            {
                normal = { background = _cardTexture },
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(10, 10, 0, 0)
            };

            CardTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = textColor }
            };

            Hint = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = mutedText }
            };

            StatValue = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Accent }
            };

            StatLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = mutedText }
            };

            Row = new GUIStyle
            {
                padding = new RectOffset(0, 0, 3, 3)
            };

            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 28f,
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(12, 12, 4, 4),
                normal = { background = Rounded(Color.white), textColor = Color.white },
                hover = { background = Rounded(new Color(0.9f, 0.9f, 0.9f)), textColor = Color.white },
                active = { background = Rounded(new Color(0.75f, 0.75f, 0.75f)), textColor = Color.white },
                focused = { background = Rounded(Color.white), textColor = Color.white }
            };

            _pill = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(8, 8, 1, 1),
                normal = { background = Rounded(Color.white), textColor = new Color(0.08f, 0.10f, 0.12f) }
            };
        }

        private static Texture2D Rounded(Color color)
        {
            const int size = 16;
            const int radius = 7;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);

                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
