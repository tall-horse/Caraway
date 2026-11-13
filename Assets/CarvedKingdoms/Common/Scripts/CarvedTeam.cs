using UnityEngine;

namespace OddJelly.CarvedKingdoms
{
    /// <summary>
    /// Sets the team colour of a Carved Kingdoms piece or unit and everything under it. It uses a MaterialPropertyBlock,
    /// so the pieces keep sharing their materials. It works in the editor too.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CarvedTeam : MonoBehaviour
    {
        public enum Side { Neutral, Red, Blue, Green, Yellow, Custom }

        public static readonly Color Red = new Color32(0xb3, 0x47, 0x3a, 0xff);
        public static readonly Color Blue = new Color32(0x4a, 0x72, 0xa8, 0xff);
        public static readonly Color Green = new Color32(0x4f, 0x8a, 0x4c, 0xff);
        public static readonly Color Yellow = new Color32(0xd8, 0xa8, 0x3a, 0xff);
        static readonly int TeamColorId = Shader.PropertyToID("_TeamColor");

        public Side side = Side.Neutral;
        [Tooltip("The colour when Side is Custom")]
        public Color custom = Color.white;

        public void SetSide(Side s)
        {
            side = s;
            Apply();
        }

        public void SetColour(Color c)
        {
            side = Side.Custom;
            custom = c;
            Apply();
        }

        public Color Colour()
        {
            switch (side)
            {
                case Side.Red: return Red;
                case Side.Blue: return Blue;
                case Side.Green: return Green;
                case Side.Yellow: return Yellow;
                case Side.Custom: return new Color(custom.r, custom.g, custom.b, 1f);
                default: return new Color(0f, 0f, 0f, 0f);
            }
        }

        void OnEnable() { Apply(); }
        void OnValidate() { Apply(); }

        public void Apply()
        {
            var block = new MaterialPropertyBlock();
            var c = Colour();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                r.GetPropertyBlock(block);
                // SetColor turns the colour into linear space by itself
                block.SetColor(TeamColorId, c);
                r.SetPropertyBlock(block);
            }
        }
    }
}
