using System.Collections;
using StickmanFight.Characters;
using StickmanFight.UI;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanFight.Core
{
    /// <summary>
    /// Правила матча (GDD §5): 3 stocks на раунд, KO — минус stock, респавн 1.4 с,
    /// победа в раунде при нуле stocks, матч до 2 побед, рематч и возврат в меню.
    /// </summary>
    public class MatchController : MonoBehaviour
    {
        [Header("Участники")]
        public StickmanCharacter player;
        public StickmanCharacter enemy;
        public Vector2 playerSpawn = new Vector2(-1.1f, 0.35f);
        public Vector2 enemySpawn = new Vector2(1.1f, 0.35f);

        [Header("Правила")]
        public int stocksPerRound = 3;
        public int roundsToWin = 2;
        public float respawnDelay = 1.4f;

        [Header("UI")]
        public StockIndicator playerStocksUi;
        public StockIndicator enemyStocksUi;
        public Text roundScoreText;
        public GameObject resultPanel;
        public Text resultTitle;
        public Text resultSubtitle;
        public Button rematchButton;
        public Button menuButton;

        private int _playerStocks;
        private int _enemyStocks;
        private int _playerRounds;
        private int _enemyRounds;
        private bool _busy;
        private bool _matchOver;

        private void Start()
        {
            _playerStocks = _enemyStocks = stocksPerRound;
            _playerRounds = _enemyRounds = 0;

            if (player != null) player.StateChanged += OnPlayerState;
            if (enemy != null) enemy.StateChanged += OnEnemyState;

            if (resultPanel != null) resultPanel.SetActive(false);
            UpdateUi();
        }

        private void OnDestroy()
        {
            if (player != null) player.StateChanged -= OnPlayerState;
            if (enemy != null) enemy.StateChanged -= OnEnemyState;
        }

        private void OnPlayerState(CharacterState state)
        {
            if (state == CharacterState.Dead) HandleKo(player);
        }

        private void OnEnemyState(CharacterState state)
        {
            if (state == CharacterState.Dead) HandleKo(enemy);
        }

        private void HandleKo(StickmanCharacter loser)
        {
            if (_busy || _matchOver) return;

            bool enemyLost = loser == enemy;
            if (enemyLost) _enemyStocks--;
            else _playerStocks--;
            UpdateUi();

            Debug.Log($"[Match] KO: {(enemyLost ? "enemy" : "player")} | stocks {_playerStocks}:{_enemyStocks}");

            if (_playerStocks <= 0 || _enemyStocks <= 0)
            {
                StartCoroutine(EndRound(enemyLost));
            }
            else
            {
                StartCoroutine(Respawn(loser, enemyLost ? enemySpawn : playerSpawn));
            }
        }

        private IEnumerator Respawn(StickmanCharacter who, Vector2 spawn)
        {
            yield return new WaitForSeconds(respawnDelay);
            if (_matchOver) yield break;
            who.Respawn(spawn);

            // После KO следующий бой начинается с полного здоровья у обоих:
            // победитель не должен сохранять остатки ХП.
            if (player != null) player.Heal();
            if (enemy != null) enemy.Heal();
        }

        private IEnumerator EndRound(bool enemyLost)
        {
            _busy = true;

            // Победитель раунда — противник проигравшего.
            bool enemyWon = !enemyLost;
            if (enemyWon) _enemyRounds++;
            else _playerRounds++;
            UpdateUi();

            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultTitle != null) resultTitle.text = enemyWon ? "РАУНД — ЖЁЛТЫЙ" : "РАУНД — СИНИЙ";
            if (resultSubtitle != null) resultSubtitle.text = "…";
            if (rematchButton != null) rematchButton.gameObject.SetActive(false);
            if (menuButton != null) menuButton.gameObject.SetActive(false);

            yield return new WaitForSeconds(1.9f);

            if (_playerRounds >= roundsToWin || _enemyRounds >= roundsToWin)
            {
                ShowMatchResult(enemyWon);
                yield break;
            }

            // Следующий раунд.
            _playerStocks = _enemyStocks = stocksPerRound;
            UpdateUi();
            if (resultPanel != null) resultPanel.SetActive(false);

            player.Respawn(playerSpawn);
            enemy.Respawn(enemySpawn);

            _busy = false;
        }

        private void ShowMatchResult(bool enemyWon)
        {
            _matchOver = true;
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultTitle != null) resultTitle.text = enemyWon ? "ПОБЕДА ЖЁЛТОГО" : "ПОБЕДА СИНЕГО";
            if (resultSubtitle != null) resultSubtitle.text = $"счёт по раундам {_playerRounds} : {_enemyRounds}";
            if (rematchButton != null) rematchButton.gameObject.SetActive(true);
            if (menuButton != null) menuButton.gameObject.SetActive(true);
        }

        private void UpdateUi()
        {
            if (playerStocksUi != null) playerStocksUi.SetStocks(_playerStocks);
            if (enemyStocksUi != null) enemyStocksUi.SetStocks(_enemyStocks);
            if (roundScoreText != null) roundScoreText.text = $"{_playerRounds} : {_enemyRounds}";
        }

        public void OnRematch()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        public void OnMenu()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("10_MainMenu");
        }
    }
}
