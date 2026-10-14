using UnityEngine;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public class GearIcon : VisualElement
    {
        const int Teeth = 8;
        const int HoleSegments = 24;

        public GearIcon()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(_ => MarkDirtyRepaint());
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 2f || rect.height < 2f) return;

            var painter = context.painter2D;
            var center = rect.center;
            var outer = Mathf.Min(rect.width, rect.height) * 0.5f;
            var inner = outer * 0.74f;
            var hole = outer * 0.32f;
            var step = Mathf.PI * 2f / Teeth;

            painter.fillColor = resolvedStyle.color;
            painter.BeginPath();
            for (var i = 0; i < Teeth; i++)
            {
                var angle = i * step;
                AddPoint(painter, center, inner, angle - step * 0.30f, i == 0);
                AddPoint(painter, center, outer, angle - step * 0.17f, false);
                AddPoint(painter, center, outer, angle + step * 0.17f, false);
                AddPoint(painter, center, inner, angle + step * 0.30f, false);
            }
            painter.ClosePath();

            for (var i = 0; i < HoleSegments; i++)
            {
                AddPoint(painter, center, hole, i * Mathf.PI * 2f / HoleSegments, i == 0);
            }
            painter.ClosePath();
            painter.Fill(FillRule.OddEven);
        }

        static void AddPoint(Painter2D painter, Vector2 center, float radius, float angle, bool first)
        {
            var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (first) painter.MoveTo(point);
            else painter.LineTo(point);
        }
    }
}
