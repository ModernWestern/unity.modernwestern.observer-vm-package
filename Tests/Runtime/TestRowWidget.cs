using UnityEngine.UI;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public sealed class TestItem
    {
        public TestItem(string name)
        {
            Name = new Observable<string>(name);
        }

        public Observable<string> Name { get; }

        public override string ToString() => Name.Value;
    }

    public sealed class TestRowWidget : Widget<TestItem>
    {
        [BindWidget] public Text Label;

        public int BindCount;

        protected override void OnBind(TestItem item)
        {
            BindCount++;

            Bind.Text(Label, item.Name);
        }
    }
}
