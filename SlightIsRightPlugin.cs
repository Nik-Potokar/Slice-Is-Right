using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.IoC;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SliceIsRight
{
    public sealed class SliceIsRightPlugin : IDalamudPlugin
    {
        public string Name => "Slice is Right";

        [PluginService]
        [RequiredVersion("1.0")]
        private DalamudPluginInterface PluginInterface { get; set; }

        [PluginService]
        private ObjectTable ObjectTable { get; set; }

        [PluginService]
        private GameGui GameGui { get; set; }

        [PluginService]
        private ClientState ClientState { get; set; }

        private const ushort GoldSaucerTerritoryId = 144;
        private bool IsInGoldSaucer { get; set; }

        public SliceIsRightPlugin()
        {
            PluginInterface!.UiBuilder.Draw += DrawUI;
            ClientState!.TerritoryChanged += TerritoryChanged;
            IsInGoldSaucer = ClientState.TerritoryType == GoldSaucerTerritoryId;
        }

        private void TerritoryChanged(object? sender, ushort e)
        {
            IsInGoldSaucer = e == GoldSaucerTerritoryId;
        }

        public void Dispose()
        {
            PluginInterface.UiBuilder.Draw -= DrawUI;
            ClientState.TerritoryChanged -= TerritoryChanged;
        }

        private void DrawUI()
        {
            /*
            if (ImGui.Begin("Debug", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoInputs))
            {
                ImGui.Text("Current zone: " + this.ClientState.TerritoryType);
                ImGui.Text("Logged in: " + this.ClientState.IsLoggedIn + ", InGS: " + this.IsInGoldSaucer);
                ImGui.End();
            }
            */
            if (!ClientState.IsLoggedIn || !IsInGoldSaucer || ObjectTable == null)
                return;


            for (int index = 0; index < ObjectTable.Length; ++ index)
            {
                GameObject? obj = ObjectTable[index];
                if (obj == null)
                    continue;

                int model = Marshal.ReadInt32(obj.Address + 128);
                if (obj.ObjectKind == ObjectKind.EventObj && (model >= 2010777 && model <= 2010779))
                {
                    //this.DebugObject(index, obj, model);
                    RenderObject(index, obj, model);
                }
                else if (ClientState.LocalPlayer?.ObjectId == obj.ObjectId)
                {
                    // local player
                    //this.DebugObject(index, obj, model);
                    //this.RenderObject(index, obj, 2010777);
                }
            }
        }


        private void DebugObject(int index, GameObject obj, int model)
        {
            if (GameGui.WorldToScreen(obj.Position, out var screenCoords))
            {
                // So, while WorldToScreen will return false if the point is off of game client screen, to
                // to avoid performance issues, we have to manually determine if creating a window would
                // produce a new viewport, and skip rendering it if so
                var objectText = $"{obj.Address.ToInt64():X}:{obj.ObjectId:X}[{index}] - {obj.ObjectKind} - {model}: {obj.Name}";

                var screenPos = ImGui.GetMainViewport().Pos;
                var screenSize = ImGui.GetMainViewport().Size;

                var windowSize = ImGui.CalcTextSize(objectText);

                // Add some extra safety padding
                windowSize.X += ImGui.GetStyle().WindowPadding.X + 10;
                windowSize.Y += ImGui.GetStyle().WindowPadding.Y + 10;

                if (screenCoords.X + windowSize.X > screenPos.X + screenSize.X ||
                    screenCoords.Y + windowSize.Y > screenPos.Y + screenSize.Y)
                    return;

                if (obj.YalmDistanceX > 20f)
                    return;

                ImGui.SetNextWindowPos(new Vector2(screenCoords.X, screenCoords.Y));

                ImGui.SetNextWindowBgAlpha(Math.Max(1f - (obj.YalmDistanceX / 20f), 0.2f));
                if (ImGui.Begin(
                        $"Actor{index}##ActorWindow{index}",
                        ImGuiWindowFlags.NoDecoration |
                        ImGuiWindowFlags.AlwaysAutoResize |
                        ImGuiWindowFlags.NoSavedSettings |
                        ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.NoMouseInputs |
                        ImGuiWindowFlags.NoDocking |
                        ImGuiWindowFlags.NoFocusOnAppearing |
                        ImGuiWindowFlags.NoNav))
                    ImGui.Text(objectText);
                ImGui.End();
            }
        }

        private void RenderObject(int index, GameObject obj, int model)
        {
            ImGui.PushID("booWindow" + index);

            ImGui.PushStyleVar((ImGuiStyleVar)1, new Vector2(0.0f, 0.0f));
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0.0f, 0.0f), ImGuiCond.None, new Vector2());
            ImGui.Begin("drawCtx" + index, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs);
            ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

            switch (model)
            {
                case 2010777:
                    DrawRectWorld(obj.Position, obj.Rotation + 1.570796f, 25f, 5f, ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 1f, 0.15f))));
                    break;

                case 2010778:
                    DrawRectWorld(obj.Position, obj.Rotation + 1.570796f, 25f, 5f, ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1f, 0.0f, 0.15f))));
                    DrawRectWorld(obj.Position, obj.Rotation - 1.570796f, 25f, 5f, ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1f, 0.0f, 0.15f))));
                    break;

                case 2010779:
                    DrawFilledCircleWorld(obj.Position, 11f, ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 0.4f))));
                    break;
            }

            ImGui.End();
            ImGui.PopStyleVar();

            ImGui.PopID();
        }

        private void DrawFilledCircleWorld(Vector3 center, float radius, uint colour)
        {
            int num = 100;
            for (int index = 0; index <= 200; ++index)
            {
                GameGui.WorldToScreen(new Vector3(center.X + radius * (float)Math.Sin(Math.PI / num * index), center.Y, center.Z + radius * (float)Math.Cos(Math.PI / num * index)), out Vector2 vector2);
                ImGui.GetWindowDrawList().PathLineTo(vector2);
            }
            ImGui.GetWindowDrawList().PathFillConvex(colour);
        }

        private void DrawRectWorld(Vector3 center, float rotation, float length, float width, uint colour)
        {
            Vector2 vector2_1 = ImGui.GetIO().DisplaySize;
            Vector3 vector3_1 = new Vector3(center.X + width * 0.5f * (float)Math.Sin(Math.PI / 2.0 + rotation), center.Y, center.Z + width * 0.5f * (float)Math.Cos(Math.PI / 2.0 + rotation));
            Vector3 vector3_2 = new Vector3(center.X + width * 0.5f * (float)Math.Sin(rotation - Math.PI / 2.0), center.Y, center.Z + width * 0.5f * (float)Math.Cos(rotation - Math.PI / 2.0));
            Vector3 vector3_3 = new Vector3(center.X, center.Y, center.Z);
            int num1 = 200;
            double num2 = (double)length / (double)num1;

            var drawList = ImGui.GetWindowDrawList();
            for (int index = 1; index <= num1; ++index)
            {
                Vector3 vector3_4 = vector3_1 = new Vector3((float)(vector3_1.X + length / (double)num1 * Math.Sin(0.0 + rotation)), vector3_1.Y, (float)(vector3_1.Z + length / (double)num1 * Math.Cos(0.0 + rotation)));
                Vector3 vector3_5 = vector3_2 = new Vector3((float)(vector3_2.X + length / (double)num1 * Math.Sin(0.0 + rotation)), vector3_2.Y, (float)(vector3_2.Z + length / (double)num1 * Math.Cos(0.0 + rotation)));
                Vector3 vector3_6 = vector3_3 = new Vector3((float)(vector3_3.X + length / (double)num1 * Math.Sin(0.0 + rotation)), vector3_3.Y, (float)(vector3_3.Z + length / (double)num1 * Math.Cos(0.0 + rotation)));

                GameGui.WorldToScreen(vector3_5, out Vector2 vector2_2);
                if (vector2_2.X > 0.0 & vector2_2.X < (double)vector2_1.X | vector2_2.Y > 0.0 & vector2_2.Y < (double)vector2_1.Y)
                {
                    drawList.PathLineTo(new Vector2((float)vector2_2.X, (float)vector2_2.Y));
                }

                GameGui.WorldToScreen(vector3_6, out vector2_2);
                if (vector2_2.X > 0.0 & vector2_2.X < (double)vector2_1.X | vector2_2.Y > 0.0 & vector2_2.Y < (double)vector2_1.Y)
                {
                    drawList.PathLineTo(new Vector2((float)vector2_2.X, (float)vector2_2.Y));
                }

                GameGui.WorldToScreen(vector3_4, out vector2_2);
                if (vector2_2.X > 0.0 & vector2_2.X < (double)vector2_1.X | vector2_2.Y > 0.0 & vector2_2.Y < (double)vector2_1.Y)
                {
                    drawList.PathLineTo(new Vector2((float)vector2_2.X, (float)vector2_2.Y));
                }

                GameGui.WorldToScreen(vector3_1, out vector2_2);
                if (vector2_2.X > 0.0 & vector2_2.X < (double)vector2_1.X | vector2_2.Y > 0.0 & vector2_2.Y < (double)vector2_1.Y)
                {
                    drawList.PathLineTo(new Vector2((float)vector2_2.X, (float)vector2_2.Y));
                }

                GameGui.WorldToScreen(vector3_3, out vector2_2);
                if (vector2_2.X > 0.0 & vector2_2.X < (double)vector2_1.X | vector2_2.Y > 0.0 & vector2_2.Y < (double)vector2_1.Y)
                {
                    drawList.PathLineTo(new Vector2((float)vector2_2.X, (float)vector2_2.Y));
                }

                GameGui.WorldToScreen(vector3_2, out vector2_2);
                if (vector2_2.X > 0.0 & vector2_2.X < (double)vector2_1.X | vector2_2.Y > 0.0 & vector2_2.Y < (double)vector2_1.Y)
                {
                    drawList.PathLineTo(new Vector2((float)vector2_2.X, (float)vector2_2.Y));
                }

                drawList.PathFillConvex(colour);
            }
        }
    }
}
