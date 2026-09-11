using UnityEngine;
using UnityEngine.UI;

namespace PaketPanik
{
    // A small untextured rounded panel. No downloaded art, atlas, or material is needed.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PanicSurface : MaskableGraphic
    {
        public float radius = 24;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            float corner = Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f);
            mesh.AddVert(r.center, color, Vector2.zero);
            const int segments = 8;
            for (int c = 0; c < 4; c++)
            {
                Vector2 center = new Vector2(c < 2 ? r.xMax - corner : r.xMin + corner,
                    c == 0 || c == 3 ? r.yMax - corner : r.yMin + corner);
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (90 - c * 90 - i * 90f / segments) * Mathf.Deg2Rad;
                    mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * corner, color, Vector2.zero);
                }
            }
            int count = 4 * (segments + 1);
            for (int i = 1; i <= count; i++) mesh.AddTriangle(0, i, i == count ? 1 : i + 1);
        }
    }
}
