namespace neoludic.QuickHyphenation
{
    public class HyphenationProcessor : BaseHyphenationProcessor
    {
        protected override void OnEnable()
        {
            Hyphenation.onHyphenationChanged += UpdateHyphenation;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Hyphenation.onHyphenationChanged -= UpdateHyphenation;
            base.OnDisable();
        }

        private void UpdateHyphenation()
        {
            textReference.SetAllDirty();
        }

        protected override string Hyphenate(string source)
        {
            if (Hyphenation.IsSetup) return source.Hyphenate();
            if (!Hyphenation.IsLoading) Hyphenation.SetupAsync();
            return source;
        }
    }
}