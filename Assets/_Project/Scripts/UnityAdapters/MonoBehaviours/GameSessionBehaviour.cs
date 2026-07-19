#nullable enable

using System;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Towers;
using RandomTowerDefense.Data.Definitions;
using RandomTowerDefense.Data.Runtime;
using RandomTowerDefense.UnityAdapters.Views;
using UnityEngine;

namespace RandomTowerDefense.UnityAdapters.MonoBehaviours
{
    [DisallowMultipleComponent]
    public sealed class GameSessionBehaviour : MonoBehaviour
    {
        [SerializeField]
        private StageDefinitionAsset? _stage;

        [SerializeField]
        private EnemyBoardView? _board;

        [SerializeField]
        private TowerBoardView? _towerBoard;

        [SerializeField]
        private ProjectileBoardView? _projectileBoard;

        [SerializeField]
        private long _seed = 42;

        private GameSession? _session;

        public GameSession Session => _session
            ?? throw new InvalidOperationException("The game session has not been initialized.");

        public EnemyBoardView Board => _board
            ?? throw new InvalidOperationException("The enemy board is not configured.");

        public TowerBoardView TowerBoard => _towerBoard
            ?? throw new InvalidOperationException("The tower board is not configured.");

        public ProjectileBoardView ProjectileBoard => _projectileBoard
            ?? throw new InvalidOperationException("The projectile board is not configured.");

        private void Awake()
        {
            if (_stage == null)
            {
                throw new InvalidOperationException("A stage definition is required.");
            }

            if (_board == null)
            {
                throw new InvalidOperationException("An enemy board view is required.");
            }

            if (_towerBoard == null)
            {
                throw new InvalidOperationException("A tower board view is required.");
            }

            if (_projectileBoard == null)
            {
                throw new InvalidOperationException("A projectile board view is required.");
            }

            if (_seed < 0)
            {
                throw new InvalidOperationException("The session seed must not be negative.");
            }

            _session = new GameSession(GameDataMapper.ToCore(_stage), (ulong)_seed);
            _board.Initialize(_stage);
            _towerBoard.Initialize(_stage);
            _projectileBoard.Initialize();
        }

        private void Start()
        {
            AdvanceSession(0f);
        }

        private void Update()
        {
            if (Session.Status == GameSessionStatus.Running)
            {
                AdvanceSession(Time.deltaTime);
            }
        }

        public GameSessionTickResult AdvanceSession(float deltaSeconds)
        {
            GameSessionTickResult result = Session.Advance(deltaSeconds);
            Board.Render(Session.ActiveEnemies);
            ProjectileBoard.Render(Session.ActiveProjectiles);
            return result;
        }

        public TowerSummonResult TrySummonTower()
        {
            TowerSummonResult result = Session.TrySummonTower();
            TowerBoard.Render(Session.TowerGrid.GetOccupiedTowers());
            return result;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            StageDefinitionAsset stage,
            EnemyBoardView board,
            TowerBoardView towerBoard,
            ProjectileBoardView projectileBoard,
            long seed)
        {
            _stage = stage;
            _board = board;
            _towerBoard = towerBoard;
            _projectileBoard = projectileBoard;
            _seed = seed;
        }
#endif
    }
}
