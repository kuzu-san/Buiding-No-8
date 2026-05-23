using UnityEngine;

namespace OfficeGame
{
    /// <summary>
    /// Глобальные настройки сложности.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDifficulty", menuName = "Game/GameDifficulty", order = 2)]
    public class GameDifficulty : ScriptableObject
    {
        public enum Level { Easy, Normal, Hard }

        [Header("Текущая сложность")]
        public Level level = Level.Normal;

        /// <summary>Множитель скорости роста шкалы настроения.</summary>
        public float MoodFillMultiplier => level switch
        {
            Level.Easy => 0.5f,
            Level.Normal => 1.0f,
            Level.Hard => 1.8f,
            _ => 1.0f
        };

        /// <summary>Множитель кулдауна работников.</summary>
        public float CooldownMultiplier => level switch
        {
            Level.Easy => 0.6f,
            Level.Normal => 1.0f,
            Level.Hard => 1.5f,
            _ => 1.0f
        };

        /// <summary>Множитель интервала между событиями.</summary>
        public float EventIntervalMultiplier => level switch
        {
            Level.Easy => 1.5f,
            Level.Normal => 1.0f,
            Level.Hard => 0.6f,
            _ => 1.0f
        };
        private static GameDifficulty _current;

        public static GameDifficulty Current
        {
            get => _current;
            set => _current = value;
        }
    }
}