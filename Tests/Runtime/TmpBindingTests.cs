#if OBSERVERVM_TMP
using TMPro;
using NUnit.Framework;

namespace ModernWestern.UI.ObserverVM.Tests
{
    public class TmpBindingTests : UiTestBase
    {
        private BindingScope _scope;

        [SetUp]
        public void CreateScope()
        {
            if (TMP_Settings.instance == null)
            {
                Assert.Ignore("TMP Essential Resources are not imported.");
            }

            _scope = new BindingScope();
        }

        [TearDown]
        public void DisposeScope()
        {
            _scope?.Dispose();
        }

        [Test]
        public void Text_String()
        {
            var label = Create<TextMeshProUGUI>();

            var source = new Observable<string>("hola");

            _scope.Text(label, source);

            Assert.AreEqual("hola", label.text);
        }

        [Test]
        public void Text_IntWithFormat()
        {
            var label = Create<TextMeshProUGUI>();

            var source = new Observable<int>(75);

            _scope.Text(label, source, "HP {0}/100");

            Assert.AreEqual("HP 75/100", label.text);
        }

        [Test]
        public void Text_FloatWithDecimals()
        {
            var label = Create<TextMeshProUGUI>();

            var source = new Observable<float>(3.14159f);

            _scope.Text(label, source, "{0:2}");

            Assert.AreEqual("3.14", label.text);
        }

        [Test]
        public void InputField_TwoWay()
        {
            var input = Create<TMP_InputField>();

            var source = new Observable<string>("hola");

            _scope.Text(input, source);

            Assert.AreEqual("hola", input.text);

            input.text = "adiós";

            Assert.AreEqual("adiós", source.Value);
        }

        [Test]
        public void Dropdown_TwoWay()
        {
            var dropdown = Create<TMP_Dropdown>();

            dropdown.options.Add(new TMP_Dropdown.OptionData("Low"));

            dropdown.options.Add(new TMP_Dropdown.OptionData("High"));

            var source = new Observable<int>(1);

            _scope.Value(dropdown, source);

            Assert.AreEqual(1, dropdown.value);

            dropdown.value = 0;

            Assert.AreEqual(0, source.Value);
        }
    }
}
#endif
