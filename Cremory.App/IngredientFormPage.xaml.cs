using Cremory.App.Models;

namespace Cremory.App
{
    public partial class IngredientFormPage : ContentPage
    {
        private readonly TaskCompletionSource<Ingredient?> _tcs = new();
        private readonly Ingredient? _existing;

        public IngredientFormPage() : this(null) { }

        public IngredientFormPage(Ingredient? existing)
        {
            InitializeComponent();
            _existing = existing;

            if (existing != null)
            {
                Title = "Edit Ingredient";
                NameEntry.Text = existing.Name;
                UnitEntry.Text = existing.Unit;
                StockEntry.Text = existing.StockQuantity.ToString();
                ReorderEntry.Text = existing.ReorderLevel.ToString();
            }
            else
            {
                Title = "New Ingredient";
            }
        }

        public Task<Ingredient?> Result => _tcs.Task;

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var name = NameEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Validation", "Ingredient name is required.", "OK");
                return;
            }

            var unit = UnitEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(unit))
            {
                await DisplayAlert("Validation", "Unit is required.", "OK");
                return;
            }

            if (!decimal.TryParse(StockEntry?.Text?.Trim(), out var stock))
            {
                await DisplayAlert("Validation", "Enter a valid stock quantity.", "OK");
                return;
            }

            if (!decimal.TryParse(ReorderEntry?.Text?.Trim(), out var reorder))
            {
                await DisplayAlert("Validation", "Enter a valid reorder level.", "OK");
                return;
            }

            var ingredient = _existing ?? new Ingredient();
            ingredient.Name = name;
            ingredient.Unit = unit;
            ingredient.StockQuantity = stock;
            ingredient.ReorderLevel = reorder;

            _tcs.TrySetResult(ingredient);
            await Navigation.PopModalAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            _tcs.TrySetResult(null);
            await Navigation.PopModalAsync();
        }
    }
}
