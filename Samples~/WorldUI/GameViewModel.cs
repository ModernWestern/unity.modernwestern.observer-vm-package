namespace ModernWestern.UI.ObserverVM.Samples.WorldUI
{
    // What the HUD shows: the player's stats plus everything the world changes when you use things in it.
    public sealed class GameViewModel
    {
        private readonly Observable<string> _message = new(string.Empty);

        public GameViewModel(CharacterViewModel player, int chestCount)
        {
            Player = player;

            ChestCount = chestCount;
        }

        public CharacterViewModel Player { get; }

        public int ChestCount { get; }

        public Observable<int> Gold { get; } = new();

        public Observable<int> ChestsOpened { get; } = new();

        public Observable<int> Defeated { get; } = new();

        public IReadOnlyObservable<string> Message => _message;

        // Raised even when the text repeats ("Not enough mana" twice), so the HUD shows it again.
        public void ShowMessage(string text)
        {
            _message.SetValueWithoutNotify(text);

            _message.Notify();
        }
    }
}
