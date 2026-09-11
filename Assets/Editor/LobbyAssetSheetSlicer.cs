using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Repeatable extraction of the user-supplied lobby sprite sheets.</summary>
public static class LobbyAssetSheetSlicer
{
    private const string SourceRoot = "Assets/UI/MultiplayerLobby/SourceSheets/";
    private const string OutputRoot = "Assets/UI/MultiplayerLobby/Sprites/";

    [MenuItem("Tools/Multiplayer/Slice Supplied Lobby Sheets")]
    public static void SliceAll()
    {
        Directory.CreateDirectory(OutputRoot);
        SliceRoundCards();
        SliceMultiplayerControls();
        SliceMainMenuHeader();
        AssetDatabase.Refresh();
        ConfigureAllOutputSprites();
        AssetDatabase.SaveAssets();
        Debug.Log("[LobbyUI] Supplied card/control sheets sliced into same-sized reusable UI sprites.");
    }

    private static void SliceRoundCards()
    {
        Texture2D sheet = Load(SourceRoot + "round_cards_sheet.png");
        const int width = 627;
        Crop(sheet, 0, 0, width, 836, "round_card_1_cloudy.png");
        Crop(sheet, width, 0, width, 836, "round_card_2_day.png");
        Crop(sheet, width * 2, 0, width, 836, "round_card_3_night.png");
        Object.DestroyImmediate(sheet);
    }

    private static void SliceMultiplayerControls()
    {
        Texture2D sheet = Load(SourceRoot + "multiplayer_controls_sheet.png");
        Crop(sheet, 0, 0, 520, 1190, "mp_panel_base.png");
        Crop(sheet, 515, 75, 456, 140, "mp_header.png");
        Crop(sheet, 520, 220, 451, 145, "mp_solo.png");
        Crop(sheet, 520, 360, 451, 145, "mp_host.png");
        Crop(sheet, 515, 505, 400, 105, "mp_join_header.png");

        Crop(sheet, 12, 1265, 238, 88, "mp_start_default.png");
        Crop(sheet, 250, 1265, 240, 88, "mp_start_hover.png");
        Crop(sheet, 490, 1265, 240, 88, "mp_start_pressed.png");
        Crop(sheet, 730, 1265, 241, 88, "mp_start_disabled.png");
        Crop(sheet, 12, 1350, 238, 88, "mp_leave_default.png");
        Crop(sheet, 250, 1350, 240, 88, "mp_leave_hover.png");
        Crop(sheet, 490, 1350, 240, 88, "mp_leave_pressed.png");
        Crop(sheet, 730, 1350, 241, 88, "mp_leave_disabled.png");

        Crop(sheet, 12, 1490, 175, 70, "mp_input_default.png");
        Crop(sheet, 187, 1490, 175, 70, "mp_input_hover.png");
        Crop(sheet, 362, 1490, 175, 70, "mp_input_pressed.png");
        Crop(sheet, 535, 1490, 175, 70, "mp_input_disabled.png");
        Crop(sheet, 545, 1490, 108, 70, "mp_join_default.png");
        Crop(sheet, 653, 1490, 108, 70, "mp_join_hover.png");
        Crop(sheet, 761, 1490, 105, 70, "mp_join_pressed.png");
        Crop(sheet, 866, 1490, 105, 70, "mp_join_disabled.png");
        Object.DestroyImmediate(sheet);
    }

    private static void SliceMainMenuHeader()
    {
        Texture2D sheet = Load(SourceRoot + "main_menu_reference.png");
        Crop(sheet, 660, 155, 350, 100, "main_menu_label.png");
        Object.DestroyImmediate(sheet);

        Texture2D transparentBanner = Load(OutputRoot + "stand_your_ground_zombie_siege_banner.png");
        Crop(transparentBanner, 0, 0, 1660, 724, "main_menu_title_only.png");
        Object.DestroyImmediate(transparentBanner);
    }

    private static Texture2D Load(string path)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(path), false))
            throw new InvalidDataException($"Could not decode {path}.");
        return texture;
    }

    private static void Crop(Texture2D source, int left, int top, int width, int height, string outputName)
    {
        int bottom = source.height - top - height;
        Color[] pixels = source.GetPixels(left, bottom, width, height);
        Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, false);
        output.SetPixels(pixels);
        output.Apply(false, false);
        File.WriteAllBytes(OutputRoot + outputName, output.EncodeToPNG());
        Object.DestroyImmediate(output);
    }

    private static void ConfigureAllOutputSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { OutputRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }
    }
}
