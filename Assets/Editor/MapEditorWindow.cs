using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class MapEditorWindow : EditorWindow
{
    private LevelData currentLevelData;
    private string selectedTileId;
    private Vector2 scrollPosition;
    private bool isPaintingMode = true;
    private bool isEraseMode = false;

    [MenuItem("Tools/2D Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorWindow>("2D Map Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        DrawLevelHeader();
        DrawTilePalette();
        DrawTools();
        DrawCurrentLevelInfo();
    }

    private void DrawLevelHeader()
    {
        GUILayout.BeginHorizontal();
        currentLevelData = (LevelData)EditorGUILayout.ObjectField("Level Data", currentLevelData, typeof(LevelData), false);

        if (GUILayout.Button("New", GUILayout.Width(60)))
        {
            CreateNewLevel();
        }

        if (GUILayout.Button("Save", GUILayout.Width(60)) && currentLevelData != null)
        {
            SaveLevel();
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    private void DrawTilePalette()
    {
        GUILayout.Label("Tile Palette", EditorStyles.boldLabel);

        if (GUILayout.Button("Ground Tile"))
        {
            selectedTileId = "ground";
            isPaintingMode = true;
            isEraseMode = false;
        }

        if (GUILayout.Button("Wall Tile"))
        {
            selectedTileId = "wall";
            isPaintingMode = true;
            isEraseMode = false;
        }

        if (GUILayout.Button("Coin"))
        {
            selectedTileId = "coin";
            isPaintingMode = true;
            isEraseMode = false;
        }

        GUILayout.Label($"Selected: {selectedTileId ?? "None"}");
        GUILayout.Space(10);
    }

    private void DrawTools()
    {
        GUILayout.Label("Tools", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Paint Tool (P)"))
        {
            isPaintingMode = true;
            isEraseMode = false;
        }

        if (GUILayout.Button("Erase Tool (E)"))
        {
            isPaintingMode = false;
            isEraseMode = true;
            selectedTileId = null;
        }
        GUILayout.EndHorizontal();

        GUILayout.Label($"Mode: {(isPaintingMode ? "PAINTING" : isEraseMode ? "ERASING" : "SELECT")}");
        GUILayout.Space(10);
    }

    private void DrawCurrentLevelInfo()
    {
        if (currentLevelData != null)
        {
            GUILayout.Label("Current Level Info", EditorStyles.boldLabel);
            GUILayout.Label($"Name: {currentLevelData.levelName}");
            GUILayout.Label($"Tiles: {currentLevelData.tiles.Count}");

            Vector2Int gridSize = currentLevelData.CalculateGridSize();
            GUILayout.Label($"Grid: {gridSize.x}x{gridSize.y} (auto)");
        }
    }

    private void CreateNewLevel()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }

        LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();
        AssetDatabase.CreateAsset(newLevel, "Assets/Data/NewLevel.asset");
        AssetDatabase.SaveAssets();
        currentLevelData = newLevel;

        Debug.Log("Создан новый уровень: " + AssetDatabase.GetAssetPath(newLevel));
    }

    private void SaveLevel()
    {
        EditorUtility.SetDirty(currentLevelData);
        AssetDatabase.SaveAssets();
        Debug.Log("Уровень сохранен: " + currentLevelData.name);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (currentLevelData == null) return;

        HandleSceneInput();
        DrawGrid();
    }

    private void HandleSceneInput()
    {
        Event e = Event.current;

        Vector2 mousePosition = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
        Vector2Int gridPosition = WorldToGridPosition(mousePosition);

        DrawGridPreview(gridPosition);

        Vector2Int gridSize = currentLevelData.CalculateGridSize();

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (isPaintingMode && !string.IsNullOrEmpty(selectedTileId))
            {
                PlaceTile(gridPosition, gridSize);
                e.Use();
            }
            else if (isEraseMode)
            {
                RemoveTile(gridPosition);
                e.Use();
            }
        }

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            if (isPaintingMode && !string.IsNullOrEmpty(selectedTileId))
            {
                PlaceTile(gridPosition, gridSize);
                e.Use();
            }
            else if (isEraseMode)
            {
                RemoveTile(gridPosition);
                e.Use();
            }
        }
    }

    private Vector2Int WorldToGridPosition(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x),
            Mathf.RoundToInt(worldPos.y)
        );
    }

    private void DrawGrid()
    {
        if (currentLevelData == null) return;

        Vector2Int gridSize = currentLevelData.CalculateGridSize();
        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        for (int x = 0; x <= gridSize.x; x++)
        {
            Vector3 start = new Vector3(x, 0, 0);
            Vector3 end = new Vector3(x, gridSize.y, 0);
            Handles.DrawLine(start, end);
        }

        for (int y = 0; y <= gridSize.y; y++)
        {
            Vector3 start = new Vector3(0, y, 0);
            Vector3 end = new Vector3(gridSize.x, y, 0);
            Handles.DrawLine(start, end);
        }
    }

    private void DrawGridPreview(Vector2Int gridPos)
    {
        if (isPaintingMode && !string.IsNullOrEmpty(selectedTileId))
        {
            Handles.color = Color.green;
        }
        else if (isEraseMode)
        {
            Handles.color = Color.red;
        }
        else
        {
            Handles.color = Color.blue;
        }

        Handles.DrawWireCube(new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0), Vector3.one);
    }

    private void PlaceTile(Vector2Int position, Vector2Int gridSize)
    {
        if (position.x < 0 || position.x >= gridSize.x ||
            position.y < 0 || position.y >= gridSize.y)
        {
            return;
        }

        TileData existingTile = currentLevelData.tiles.Find(t => t.position == position);

        if (existingTile != null)
        {
            existingTile.tileId = selectedTileId;
        }
        else
        {
            currentLevelData.tiles.Add(new TileData
            {
                position = position,
                tileId = selectedTileId,
                layer = 0
            });
        }

        EditorUtility.SetDirty(currentLevelData);
        Repaint();
    }

    private void RemoveTile(Vector2Int position)
    {
        int removedCount = currentLevelData.tiles.RemoveAll(t => t.position == position);
        if (removedCount > 0)
        {
            EditorUtility.SetDirty(currentLevelData);
            Repaint();
        }
    }
}