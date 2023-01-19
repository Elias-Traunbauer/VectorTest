using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        DebugAPI Draw;

        public Program()
        {
            Draw = new DebugAPI(this);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            //PlaneD plane = new PlaneD(Vector3D.Backward, 0);

            //Vector3D origin = Vector3D.Backward * 2.5d / 2d;
            //Vector3D target = new Vector3D(1, 1, -1);
            //Vector3D direction = -(target - origin);
            //Vector3D intersection = plane.Intersection(ref origin, ref direction) + Vector3D.Forward * 2.5d / 2d;

            //Vector3D drawPos = Me.GetPosition() + LocalDirToWorldDir(Vector3D.Up * 5, Me.WorldMatrix);
            //MatrixD offsetMatrix = Me.WorldMatrix;
            //offsetMatrix.Translation = offsetMatrix.Translation + LocalDirToWorldDir(Vector3D.Up * 5, Me.WorldMatrix);
            //Draw.DrawPoint(drawPos, Color.Gray, 0.05f, 30);
            //Draw.DrawLine(drawPos, drawPos + LocalDirToWorldDir(direction, offsetMatrix), Color.White, 0.01f, 30);

            //Draw.DrawPoint(LocalPosToWorldPos(Vector3D.Forward * 2.5d / 2d, offsetMatrix), Color.Green, 0.05f, 30);
            //Draw.DrawLine(LocalPosToWorldPos(Vector3D.Forward * 2.5d / 2d, offsetMatrix), LocalPosToWorldPos(Vector3D.Forward * 2.5d / 2d + Vector3D.Up * 2.5d, offsetMatrix), Color.Blue, 0.01f, 30);
            //Draw.DrawLine(LocalPosToWorldPos(Vector3D.Forward * 2.5d / 2d, offsetMatrix), LocalPosToWorldPos(Vector3D.Forward * 2.5d / 2d + Vector3D.Right * 2.5d, offsetMatrix), Color.Red, 0.01f, 30);

            //Draw.DrawPoint(LocalPosToWorldPos(intersection, offsetMatrix), Color.Red, 0.05f, 30);
            // Forward is looking at dir
            var lcd = GridTerminalSystem.GetBlockWithName("TestLCD") as IMyTextPanel;
            TextPanelRenderingContext context = new TextPanelRenderingContext(ref lcd, Vector3D.Backward * 5, Draw);
            var res = context.ProjectPoint(lcd.GetPosition() + Vector3D.TransformNormal(Vector3D.Forward * 3 + Vector3D.Right, lcd.WorldMatrix));
            lcd.ContentType = ContentType.SCRIPT;

            var df = lcd.DrawFrame();
            df.Add(new MySprite()
            {
                Data = "SquareSimple",
                Type = SpriteType.TEXTURE,
                Color = Color.Red,
                Position = res,
                Size = new Vector2(3, 3)
            }) ;
            df.Dispose();
            Echo(res.ToString());
        }

        /// <summary>
        /// Class to project points from a position onto an lcd
        /// NOTE: Only works if the ViewPoint is infront of the lcd -> Transparent LCDS from the back dont work
        /// </summary>
        public class TextPanelRenderingContext
        {
            DebugAPI Draw;
            public Vector3D ViewPoint { get; private set; }
            public IMyTextPanel TextPanel { get; private set; }
            public Vector2 PixelMultiplier { get; private set; }

            private readonly Vector3D Normal = Vector3D.Backward;
            public readonly double TextPanelThickness = 0.2f;

            /// <summary>
            /// Initializes the renderer to a working state
            /// </summary>
            /// <param name="lcd">The lcd you want to project to</param>
            /// <param name="viewPointDirection">Direction to view from local to lcd's matrix</param>
            public TextPanelRenderingContext(ref IMyTextPanel lcd, Vector3D viewPointDirection, DebugAPI debugAPI = null)
            {
                Draw = debugAPI;
                TextPanel = lcd;
                ViewPoint = viewPointDirection + Vector3D.Backward * ((2.5d / 2d) - TextPanelThickness);

                var screenSize = GetTextPanelSizeFromGridView(TextPanel);
                PixelMultiplier = TextPanel.TextureSize / screenSize;
            }

            private static Vector2I GetTextPanelSizeFromGridView(IMyTextPanel textPanel)
            {
                Vector3I lcdSize = textPanel.Max - textPanel.Min;
                Vector2I screenSize = new Vector2I();
                switch (textPanel.Orientation.Forward)
                {
                    case Base6Directions.Direction.Forward:
                        screenSize = new Vector2I(lcdSize.X, lcdSize.Y);
                        break;
                    case Base6Directions.Direction.Backward:
                        screenSize = new Vector2I(lcdSize.X, lcdSize.Y);
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
                // Local view to world point
                Vector3D worldViewPos = Vector3D.Transform(ViewPoint, TextPanel.WorldMatrix);
                // direction from the viewPoint to the worldPoint
                Vector3D worldRayDirection = worldPoint - worldViewPos;
                Draw.DrawLine(worldViewPos, worldPoint, Color.White, 0.01f, 10, true);
                // ray direction in local space
                Vector3D localRayDirection = Vector3D.Transform(worldRayDirection, MatrixD.Transpose(TextPanel.WorldMatrix));
                //// we dont normalize to keep it at max performance
                //localRayDirection.Normalize();

                // project the plane onto the plane
                Vector2? projectedLocalPoint = PlaneIntersection(ViewPoint, localRayDirection);
                if (projectedLocalPoint != null)
                {
                    var projectedLocalPointNonNullable = (Vector2)projectedLocalPoint;
                    Draw.DrawLine(LocalPosToWorldPos(ViewPoint, TextPanel.WorldMatrix), TextPanel.GetPosition(), Color.Green, 0.02f, 10, true);
                    // DEBUG
                    Draw.DrawLine(worldViewPos, LocalPosToWorldPos(new Vector3D((double)projectedLocalPointNonNullable.X, (double)projectedLocalPointNonNullable.Y, -2f / 2d), TextPanel.WorldMatrix), Color.Red, 0.01f, 10, true);
                    Draw.DrawPoint(LocalPosToWorldPos(new Vector3D((double)projectedLocalPointNonNullable.X, (double)projectedLocalPointNonNullable.Y, -2f / 2d), TextPanel.WorldMatrix), Color.Red, 0.1f, 10);
                    // convert it to pixels
                    Vector2 projectedLocalPointPixels = projectedLocalPointNonNullable * PixelMultiplier;
                    projectedLocalPointPixels += TextPanel.TextureSize / 2f;
                    return projectedLocalPointPixels;
                }
                else
                {
                    return null;
                }
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
                var t = -DotNormal(origin) / DotNormal(dir);
                Vector3D res = origin + t * dir;
                return new Vector2((float)res.X, (float)res.Y);
            }

            /// <summary>
            /// Calculates the dot-product of a specified Vector3D and the normal vector (static)
            /// </summary>
            /// <param name="value">The Vector3D to calculate the dot product of</param>
            private double DotNormal(Vector3D value)
            {
                return (double)(Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z);
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

        Vector3D LocalDirToWorldDir(Vector3D dir, MatrixD matrix)
        {
            return Vector3D.TransformNormal(dir, matrix);
        }

        Vector3D LocalPosToWorldPos(Vector3D pos, MatrixD matrix)
        {
            return Vector3D.Transform(pos, matrix);
        }

        public void Save()
        {
        
        }

        public void Main(string argument, UpdateType updateSource)
        {
            List<IMyTextPanel> pnls = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(pnls);
            foreach (var item in pnls)
            {
                Draw.DrawLine(item.GetPosition(), Me.CubeGrid.GridIntegerToWorld(item.Min), Color.Blue, 0.1f, 10, true);
                Draw.DrawLine(item.GetPosition(), Me.CubeGrid.GridIntegerToWorld(item.Max), Color.Red, 0.1f, 10, true);
            }
        }
    }

    public class DebugAPI
    {
        public readonly bool ModDetected;

        public void RemoveDraw() => _removeDraw?.Invoke(_pb);
        Action<IMyProgrammableBlock> _removeDraw;

        public void RemoveAll() => _removeAll?.Invoke(_pb);
        Action<IMyProgrammableBlock> _removeAll;

        public void Remove(int id) => _remove?.Invoke(_pb, id);
        Action<IMyProgrammableBlock, int> _remove;

        public int DrawPoint(Vector3D origin, Color color, float radius = 0.2f, float seconds = DefaultSeconds, bool? onTop = null) => _point?.Invoke(_pb, origin, color, radius, seconds, onTop ?? _defaultOnTop) ?? -1;
        Func<IMyProgrammableBlock, Vector3D, Color, float, float, bool, int> _point;

        public int DrawLine(Vector3D start, Vector3D end, Color color, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _line?.Invoke(_pb, start, end, color, thickness, seconds, onTop ?? _defaultOnTop) ?? -1;
        Func<IMyProgrammableBlock, Vector3D, Vector3D, Color, float, float, bool, int> _line;

        public int DrawAABB(BoundingBoxD bb, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _aabb?.Invoke(_pb, bb, color, (int)style, thickness, seconds, onTop ?? _defaultOnTop) ?? -1;
        Func<IMyProgrammableBlock, BoundingBoxD, Color, int, float, float, bool, int> _aabb;

        public int DrawOBB(MyOrientedBoundingBoxD obb, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _obb?.Invoke(_pb, obb, color, (int)style, thickness, seconds, onTop ?? _defaultOnTop) ?? -1;
        Func<IMyProgrammableBlock, MyOrientedBoundingBoxD, Color, int, float, float, bool, int> _obb;

        public int DrawSphere(BoundingSphereD sphere, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, int lineEveryDegrees = 15, float seconds = DefaultSeconds, bool? onTop = null) => _sphere?.Invoke(_pb, sphere, color, (int)style, thickness, lineEveryDegrees, seconds, onTop ?? _defaultOnTop) ?? -1;
        Func<IMyProgrammableBlock, BoundingSphereD, Color, int, float, int, float, bool, int> _sphere;

        public int DrawMatrix(MatrixD matrix, float length = 1f, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _matrix?.Invoke(_pb, matrix, length, thickness, seconds, onTop ?? _defaultOnTop) ?? -1;
        Func<IMyProgrammableBlock, MatrixD, float, float, float, bool, int> _matrix;

        public int DrawGPS(string name, Vector3D origin, Color? color = null, float seconds = DefaultSeconds) => _gps?.Invoke(_pb, name, origin, color, seconds) ?? -1;
        Func<IMyProgrammableBlock, string, Vector3D, Color?, float, int> _gps;

        public int PrintHUD(string message, Font font = Font.Debug, float seconds = 2) => _printHUD?.Invoke(_pb, message, font.ToString(), seconds) ?? -1;
        Func<IMyProgrammableBlock, string, string, float, int> _printHUD;

        public void PrintChat(string message, string sender = null, Color? senderColor = null, Font font = Font.Debug) => _chat?.Invoke(_pb, message, sender, senderColor, font.ToString());
        Action<IMyProgrammableBlock, string, string, Color?, string> _chat;

        public void DeclareAdjustNumber(out int id, double initial, double step = 0.05, Input modifier = Input.Control, string label = null) => id = _adjustNumber?.Invoke(_pb, initial, step, modifier.ToString(), label) ?? -1;
        Func<IMyProgrammableBlock, double, double, string, string, int> _adjustNumber;

        public double GetAdjustNumber(int id, double noModDefault = 1) => _getAdjustNumber?.Invoke(_pb, id) ?? noModDefault;
        Func<IMyProgrammableBlock, int, double> _getAdjustNumber;

        public int GetTick() => _tick?.Invoke() ?? -1;
        Func<int> _tick;

        public enum Style { Solid, Wireframe, SolidAndWireframe }
        public enum Input { MouseLeftButton, MouseRightButton, MouseMiddleButton, MouseExtraButton1, MouseExtraButton2, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Tab, Shift, Control, Alt, Space, PageUp, PageDown, End, Home, Insert, Delete, Left, Up, Right, Down, D0, D1, D2, D3, D4, D5, D6, D7, D8, D9, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, NumPad0, NumPad1, NumPad2, NumPad3, NumPad4, NumPad5, NumPad6, NumPad7, NumPad8, NumPad9, Multiply, Add, Separator, Subtract, Decimal, Divide, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12 }
        public enum Font { Debug, White, Red, Green, Blue, DarkBlue }

        const float DefaultThickness = 0.02f;
        const float DefaultSeconds = -1;

        IMyProgrammableBlock _pb;
        bool _defaultOnTop;

        public DebugAPI(MyGridProgram program, bool drawOnTopDefault = false)
        {
            if (program == null)
                throw new Exception("Pass `this` into the API, not null.");

            _defaultOnTop = drawOnTopDefault;
            _pb = program.Me;

            var methods = _pb.GetProperty("DebugAPI")?.As<IReadOnlyDictionary<string, Delegate>>()?.GetValue(_pb);
            if (methods != null)
            {
                Assign(out _removeAll, methods["RemoveAll"]);
                Assign(out _removeDraw, methods["RemoveDraw"]);
                Assign(out _remove, methods["Remove"]);
                Assign(out _point, methods["Point"]);
                Assign(out _line, methods["Line"]);
                Assign(out _aabb, methods["AABB"]);
                Assign(out _obb, methods["OBB"]);
                Assign(out _sphere, methods["Sphere"]);
                Assign(out _matrix, methods["Matrix"]);
                Assign(out _gps, methods["GPS"]);
                Assign(out _printHUD, methods["HUDNotification"]);
                Assign(out _chat, methods["Chat"]);
                Assign(out _adjustNumber, methods["DeclareAdjustNumber"]);
                Assign(out _getAdjustNumber, methods["GetAdjustNumber"]);
                Assign(out _tick, methods["Tick"]);
                RemoveAll();
                ModDetected = true;
            }
        }

        void Assign<T>(out T field, object method) => field = (T)method;
    }
}
