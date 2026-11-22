namespace StickmanFight.Characters
{
    /// <summary>Состояния персонажа (FR-RAG-02). Переходы — только через StickmanCharacter.</summary>
    public enum CharacterState
    {
        Standing,
        Moving,
        Airborne,
        Attacking,
        Stunned,
        Knockdown,
        Dead
    }
}
