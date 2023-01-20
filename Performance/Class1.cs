using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Performance
{
    /// <summary>
    /// Class to project points from a position onto an lcd
    /// NOTE: Only works if the ViewPoint is infront of the lcd -> Transparent LCDS from the back dont work
    /// </summary>
    public class TextPanelRenderingContext
    {
        public Vector3D ViewPoint { get; private set; }
        public IMyTextPanel TextPanel { get; private set; }
        public Vector2 PixelMultiplier { get; private set; }

        private readonly Vector3D Normal = Vector3D.Backward;
        public static readonly double TextPanelThickness = 0.05f;
        public static readonly double D = 2.5d / 2d - TextPanelThickness;
        public static float TextPanelTextureMargin = 0f;
        static Program pr = null;

        /// <summary>
        /// Initializes the renderer to a working state
        /// </summary>
        /// <param name="lcd">The lcd you want to project to</param>
        /// <param name="viewPointDirection">Direction to view from local to lcd's matrix</param>
        public TextPanelRenderingContext(ref IMyTextPanel lcd, Vector3D viewPointDirection)
        {
            TextPanel = lcd;
            ViewPoint = viewPointDirection;
            // magic numbers for lcd margin
            TextPanelTextureMargin = TextPanel.BlockDefinition.SubtypeId == "TransparentLCDLarge" ? 0.33f : -0.08f;
            var screenSize = GetTextPanelSizeFromGridView(TextPanel);
            PixelMultiplier = TextPanel.TextureSize / ((Vector2)screenSize * (2.5f - TextPanelTextureMargin));
            float maxMult = PixelMultiplier.X > PixelMultiplier.Y ? PixelMultiplier.Y : PixelMultiplier.X;
            PixelMultiplier = new Vector2(maxMult, maxMult);
        }

        private static Vector2I GetTextPanelSizeFromGridView(IMyTextPanel textPanel)
        {
            Vector3I lcdSize = textPanel.Max - textPanel.Min;
            Vector2I screenSize = new Vector2I();
            switch (textPanel.Orientation.Forward)
            {
                case Base6Directions.Direction.Forward:
                    screenSize = new Vector2I(lcdSize.Y, lcdSize.X);
                    break;
                case Base6Directions.Direction.Backward:
                    screenSize = new Vector2I(lcdSize.Y, lcdSize.X);
                    break;
                case Base6Directions.Direction.Left:
                    screenSize = new Vector2I(lcdSize.Z, lcdSize.Y);
                    break;
                case Base6Directions.Direction.Right:
                    screenSize = new Vector2I(lcdSize.Z, lcdSize.Y);
                    break;
                case Base6Directions.Direction.Up:
                    screenSize = new Vector2I(lcdSize.X, lcdSize.Z);
                    break;
                case Base6Directions.Direction.Down:
                    screenSize = new Vector2I(lcdSize.X, lcdSize.Z);
                    break;
                default:
                    throw new ArgumentException("Unknown orientation");
            }
            screenSize += new Vector2I(1, 1);
            return screenSize;
        }

        /// <summary>
        /// Projects the given point onto LCD screen coordinates given in pixels
        /// </summary>
        /// <param name="worldPoint">The point to project</param>
        /// <returns>Screen coordinate in pixels or null if projection is not on lcd</returns>
        public Vector2? ProjectPoint(Vector3D worldPoint)
        {
            Vector3D referenceWorldPosition = TextPanel.WorldMatrix.Translation;
            // Convert worldPosition into a world direction
            Vector3D worldDirection = worldPoint - referenceWorldPosition;
            // Convert worldDirection into a local direction
            Vector3D localPointToProject = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(TextPanel.WorldMatrix));
            // ray direction in local space
            Vector3D localRayDirection = localPointToProject - ViewPoint;
            //// we dont normalize to keep it at max performance
            //localRayDirection.Normalize();

            // project the plane onto the plane
            Vector2? projectedLocalPoint = PlaneIntersection(ViewPoint, localRayDirection);
            if (projectedLocalPoint != null)
            {
                var projectedLocalPointNonNullable = (Vector2)projectedLocalPoint;
                // convert it to pixels
                Vector2 projectedLocalPointPixels = projectedLocalPointNonNullable * PixelMultiplier * new Vector2(1, -1);
                projectedLocalPointPixels += TextPanel.TextureSize / 2f;
                if (projectedLocalPointPixels.X >= 0 && projectedLocalPointPixels.Y >= 0 && projectedLocalPointPixels.X < TextPanel.SurfaceSize.X && projectedLocalPointPixels.Y < TextPanel.SurfaceSize.Y)
                {
                    return projectedLocalPointPixels;
                }
            }
            return null;
        }

        /// <summary>
        /// Calculates the intersection point from the given line and a plane with origin (0,0,0) and the normal (static)
        /// </summary>
        /// <param name="origin">Line origin</param>
        /// <param name="dir">Line direction</param>
        /// <returns>The projected point</returns>
        private Vector2? PlaneIntersection(Vector3D origin, Vector3D dir)
        {
            if (dir.Z >= 0)
            {
                return null;
            }
            var t = -(Vector3D.Dot(origin, Normal) + D) / Vector3D.Dot(dir, Normal);
            Vector3D res = origin + t * dir;
            return new Vector2((float)res.X, (float)res.Y);
        }

        Vector3D LocalDirToWorldDir(Vector3D dir, MatrixD matrix)
        {
            return Vector3D.TransformNormal(dir, matrix);
        }

        Vector3D LocalPosToWorldPos(Vector3D pos, MatrixD matrix)
        {
            return Vector3D.Transform(pos, matrix);
        }

    }
}
