using Cremory.App.Models;

namespace Cremory.App
{
    public partial class ProductFormPage : ContentPage
    {
        private readonly TaskCompletionSource<Product?> _tcs = new();
        private readonly Product? _existing;
        private readonly List<Category> _categories;

        public ProductFormPage(List<Category> categories) : this(categories, null) { }

        public ProductFormPage(List<Category> categories, Product? existing)
        {
            InitializeComponent();
            _existing = existing;
            _categories = categories;

            CategoryPicker.ItemsSource = categories;
            CategoryPicker.ItemDisplayBinding = new Binding("Name");

            if (existing != null)
            {
                Title = "Edit Product";
                NameEntry.Text = existing.Name;
                VariantEntry.Text = existing.Variant;
                FlavorEntry.Text = existing.Flavor;
                PriceEntry.Text = existing.BasePrice.ToString();
                ActiveSwitch.IsToggled = existing.IsActive;

                var idx = categories.FindIndex(c => c.CategoryId == existing.CategoryId);
                if (idx >= 0)
                    CategoryPicker.SelectedIndex = idx;
            }
            else
            {
                Title = "New Product";
                if (categories.Count > 0)
                    CategoryPicker.SelectedIndex = 0;
            }
        }

        public Task<Product?> Result => _tcs.Task;

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var name = NameEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Validation", "Product name is required.", "OK");
                return;
            }

            if (!decimal.TryParse(PriceEntry?.Text?.Trim(), out var price) || price <= 0)
            {
                await DisplayAlert("Validation", "Enter a valid base price greater than 0.", "OK");
                return;
            }

            if (CategoryPicker.SelectedIndex < 0)
            {
                await DisplayAlert("Validation", "Select a category.", "OK");
                return;
            }

            var product = _existing ?? new Product();
            product.Name = name;
            product.Variant = VariantEntry?.Text?.Trim();
            product.Flavor = FlavorEntry?.Text?.Trim();
            product.BasePrice = price;
            product.IsActive = ActiveSwitch.IsToggled;
            product.CategoryId = _categories[CategoryPicker.SelectedIndex].CategoryId;

            _tcs.TrySetResult(product);
            await Navigation.PopModalAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            _tcs.TrySetResult(null);
            await Navigation.PopModalAsync();
        }
    }
}
