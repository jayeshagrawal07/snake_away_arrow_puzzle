using System.Collections.Generic;
using System.IO;
using SnakeEscape.Application;
using SnakeEscape.Domain;
using SnakeEscape.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SnakeEscape.Editor
{
    public static class LevelAssetCreator
    {
        private const string LevelsFolder = "Assets/Resources/" + LevelService.LevelsResourcePath;
        private const string ScenePath = "Assets/Game/Scenes/Gameplay.unity";

        [MenuItem("Tools/Snake Escape/Setup Gameplay Scene")]
        public static void CreateGameplayScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Main Camera
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.96f, 0.96f, 0.94f); // Warm clean background per spec
            camObj.transform.position = new Vector3(0f, 0f, -10f);
            camObj.tag = "MainCamera";

            // GameController Root (BoardView, SnakeInputHandler, GameplayHUD, LevelCompleteUI auto-attached)
            var controllerObj = new GameObject("[GameController]");
            controllerObj.AddComponent<GameController>();

            // Ensure directory exists
            string dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[LevelAssetCreator] Gameplay.unity scene created and saved to " + ScenePath);
        }

        [MenuItem("Tools/Snake Escape/Generate Sample Levels")]
        public static void GenerateSampleLevels()
        {
            if (!Directory.Exists(LevelsFolder))
            {
                Directory.CreateDirectory(LevelsFolder);
            }

            AssetDatabase.DeleteAsset($"{LevelsFolder}/Level_001.asset");
            AssetDatabase.DeleteAsset($"{LevelsFolder}/Level_002.asset");
            AssetDatabase.DeleteAsset($"{LevelsFolder}/Level_003.asset");

            CreateLevel1();
            CreateLevel2();
            CreateLevel3();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LevelAssetCreator] Sample levels 1-3 created under " + LevelsFolder + " and can be loaded from Resources/Levels.");
        }

        private static void CreateLevel1()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "Level_001";
            level.CampaignChapter = "Chapter 1";
            level.SortOrder = 1;
            level.Cols = 8;
            level.Rows = 8;
            level.DifficultyCategory = "Tutorial";
            level.LearningObjective = "Learn how to untangle a snake blocked by another snake's body.";

            // Snake A (Green): at (2,3), (2,2), (2,1) facing Right
            var sA = new SnakeDefinition
            {
                Id = "snake_A",
                BodyColor = new Color(0.18f, 0.72f, 0.44f),
                ExitDirection = GridCoord.Right,
                BodyCells = new List<GridCoord>
                {
                    new GridCoord(2, 3),
                    new GridCoord(2, 2),
                    new GridCoord(2, 1)
                }
            };

            // Snake B (Blue): at (4,3), (4,2), (4,1) facing Up
            var sB = new SnakeDefinition
            {
                Id = "snake_B",
                BodyColor = new Color(0.20f, 0.50f, 0.95f),
                ExitDirection = GridCoord.Up,
                BodyCells = new List<GridCoord>
                {
                    new GridCoord(4, 3),
                    new GridCoord(4, 2),
                    new GridCoord(4, 1)
                }
            };

            level.Snakes = new List<SnakeDefinition> { sA, sB };

            // Run solver to guarantee solvable
            var solver = new PuzzleSolver();
            var solveResult = solver.Solve(level);
            level.ValidationPassed = solveResult.IsSolved;
            level.ValidationReport = solveResult.Message;

            AssetDatabase.CreateAsset(level, $"{LevelsFolder}/Level_001.asset");
        }

        private static void CreateLevel2()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "Level_002";
            level.CampaignChapter = "Chapter 1";
            level.SortOrder = 2;
            level.Cols = 8;
            level.Rows = 8;
            level.DifficultyCategory = "Easy";
            level.LearningObjective = "3-snake dependency chain: C clears B, B clears A.";

            // Snake A: blocked by B
            var sA = new SnakeDefinition
            {
                Id = "snake_A",
                BodyColor = new Color(0.18f, 0.72f, 0.44f), // Green
                ExitDirection = GridCoord.Right,
                BodyCells = new List<GridCoord>
                {
                    new GridCoord(1, 4),
                    new GridCoord(1, 3),
                    new GridCoord(1, 2)
                }
            };

            // Snake B: blocks A at (3,4), blocked by C at (3,6)
            var sB = new SnakeDefinition
            {
                Id = "snake_B",
                BodyColor = new Color(0.20f, 0.50f, 0.95f), // Blue
                ExitDirection = GridCoord.Up,
                BodyCells = new List<GridCoord>
                {
                    new GridCoord(3, 4),
                    new GridCoord(3, 3),
                    new GridCoord(3, 2)
                }
            };

            // Snake C: blocks B at (3,6), free to escape Right
            var sC = new SnakeDefinition
            {
                Id = "snake_C",
                BodyColor = new Color(0.95f, 0.45f, 0.20f), // Coral/Orange
                ExitDirection = GridCoord.Right,
                BodyCells = new List<GridCoord>
                {
                    new GridCoord(3, 6),
                    new GridCoord(2, 6),
                    new GridCoord(1, 6)
                }
            };

            level.Snakes = new List<SnakeDefinition> { sA, sB, sC };

            var solver = new PuzzleSolver();
            var solveResult = solver.Solve(level);
            level.ValidationPassed = solveResult.IsSolved;
            level.ValidationReport = solveResult.Message;

            AssetDatabase.CreateAsset(level, $"{LevelsFolder}/Level_002.asset");
        }

        private static void CreateLevel3()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "Level_003";
            level.CampaignChapter = "Chapter 1";
            level.SortOrder = 3;
            level.Cols = 8;
            level.Rows = 8;
            level.DifficultyCategory = "Easy";
            level.LearningObjective = "Curved snakes with multiple turns.";

            // Snake 1: L-shaped snake
            var s1 = new SnakeDefinition
            {
                Id = "snake_1",
                BodyColor = new Color(0.60f, 0.25f, 0.85f), // Purple
                ExitDirection = GridCoord.Up,
                BodyCells = new List<GridCoord>
                {
                    new GridCoord(2, 5),
                    new GridCoord(2, 4),
                    new GridCoord(3, 4),
                    new GridCoord(4, 4)
                }
            };

            // Snake 2: Blocks snake 1
            var s2 = new SnakeDefinition
            {
                Id = "snake_2",
                BodyColor = new Color(0.15f, 0.75f, 0.75f), // Teal
                ExitDirection = GridCoord.Right,
                BodyCells = new List<GridCoord>
                {
                    new GridCoord(2, 6),
                    new GridCoord(1, 6),
                    new GridCoord(0, 6)
                }
            };

            level.Snakes = new List<SnakeDefinition> { s1, s2 };

            var solver = new PuzzleSolver();
            var solveResult = solver.Solve(level);
            level.ValidationPassed = solveResult.IsSolved;
            level.ValidationReport = solveResult.Message;

            AssetDatabase.CreateAsset(level, $"{LevelsFolder}/Level_003.asset");
        }
    }
}
