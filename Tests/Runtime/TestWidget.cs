using UnityEngine;
using UnityEngine.UI;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public sealed class TestViewModel
    {
        public readonly Observable<float> Health = new(1f);

        public readonly Observable<Color> Tint = new(Color.white);

        public readonly Observable<float> Volume = new(0.5f);

        public readonly Observable<bool> Muted = new(false);

        public readonly Observable<int> Quality = new(0);

        public readonly Observable<string> Nickname = new("");

        public readonly Observable<bool> Visible = new(true);

        public readonly Command Respawn;

        public int Respawns;

        public TestViewModel()
        {
            Respawn = new Command(OnRespawn);
        }

        private void OnRespawn()
        {
            Respawns++;
        }
    }

    public sealed class TestWidget : Widget<TestViewModel>
    {
        [BindWidget] public Image HealthBar;

        [BindWidget] public Slider Volume;

        [BindWidget] public Toggle Muted;

        [BindWidget] public Dropdown Quality;

        [BindWidget] public Text Nickname;

        [BindWidget] public Button Respawn;

        [BindWidget(Optional = true)] public GameObject Panel;

        public int BindCount;

        public int UnbindCount;

        public TestViewModel LastUnbound;

        public float LastHealth;

        protected override void OnBind(TestViewModel viewModel)
        {
            BindCount++;

            Bind.Fill(HealthBar, viewModel.Health);

            Bind.Color(HealthBar, viewModel.Tint);

            Bind.Value(Volume, viewModel.Volume);

            Bind.IsOn(Muted, viewModel.Muted);

            Bind.Value(Quality, viewModel.Quality);

            Bind.Text(Nickname, viewModel.Nickname);

            Bind.Click(Respawn, viewModel.Respawn);

            Bind.Active(Panel, viewModel.Visible);

            Bind.To(viewModel.Health, this, static (self, health) => self.LastHealth = health);
        }

        protected override void OnUnbind(TestViewModel viewModel)
        {
            UnbindCount++;

            LastUnbound = viewModel;
        }
    }
}
