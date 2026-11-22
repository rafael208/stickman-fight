namespace StickmanFight.Inputs
{
    /// <summary>
    /// Источник намерений игрока (FR-INPUT-02). Реализации: тач (TouchInput), клавиатура (KeyboardInput),
    /// позже — ИИ. Consume*-методы возвращают «нажатие в этом кадре» и сбрасывают его.
    /// </summary>
    public interface IInputSource
    {
        float MoveX { get; }
        bool ConsumeJump();
        bool ConsumeAttack();
        bool ConsumeInteract();
        bool ConsumeDrop();
    }
}
