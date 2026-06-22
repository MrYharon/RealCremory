using System.Collections.ObjectModel;
using Cremory.App.Models;

namespace Cremory.App
{
    public partial class RecipeFormPage : ContentPage
    {
        private readonly TaskCompletionSource<Recipe?> _tcs = new();
        private readonly Recipe? _existing;
        private readonly List<Ingredient> _allIngredients;
        private readonly ObservableCollection<RecipeIngredient> _ingredients = [];

        public RecipeFormPage(List<Ingredient> allIngredients) : this(allIngredients, null) { }

        public RecipeFormPage(List<Ingredient> allIngredients, Recipe? existing)
        {
            InitializeComponent();
            _allIngredients = allIngredients;
            _existing = existing;

            IngredientsCollection.ItemsSource = _ingredients;

            if (existing != null)
            {
                Title = "Edit Recipe";
                NameEntry.Text = existing.Name;
                PriceEntry.Text = existing.SellingPrice.ToString();
                DescriptionEditor.Text = existing.Description;
                ActiveSwitch.IsToggled = existing.IsActive;

                foreach (var ri in existing.RecipeIngredients)
                    _ingredients.Add(ri);
            }
            else
            {
                Title = "New Recipe";
            }
        }

        public Task<Recipe?> Result => _tcs.Task;

        private async void OnAddIngredient(object sender, EventArgs e)
        {
            var available = _allIngredients
                .Where(i => !_ingredients.Any(ri => ri.IngredientId == i.IngredientId))
                .OrderBy(i => i.Name)
                .ToList();

            if (available.Count == 0)
            {
                await DisplayAlert("No Ingredients", "All ingredients are already added.", "OK");
                return;
            }

            if (available.Count > 20)
            {
                var search = await DisplayPromptAsync("Search Ingredient",
                    "Type to find an ingredient:",
                    accept: "Search",
                    placeholder: "e.g. Flour");
                if (string.IsNullOrWhiteSpace(search))
                {
                    var names = available.Select(i => $"{i.Name} ({i.Unit})").Prepend("Cancel").ToArray();
                    var selected = await DisplayActionSheet("Select Ingredient", "Cancel", null, names[1..]);
                    if (string.IsNullOrEmpty(selected) || selected == "Cancel") return;
                    var idx = Array.IndexOf(names, selected) - 1;
                    if (idx < 0) return;
                    await PromptQuantity(available[idx]);
                }
                else
                {
                    var matched = available
                        .Where(i => i.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (matched.Count == 0)
                    {
                        await DisplayAlert("Not Found", $"No ingredient matching \"{search}\".", "OK");
                        return;
                    }
                    if (matched.Count == 1)
                    {
                        await PromptQuantity(matched[0]);
                        return;
                    }
                    var names = matched.Select(i => $"{i.Name} ({i.Unit})").ToArray();
                    var selected = await DisplayActionSheet("Select Ingredient", "Cancel", null, names);
                    if (string.IsNullOrEmpty(selected) || selected == "Cancel") return;
                    var idx = Array.IndexOf(names, selected);
                    if (idx < 0) return;
                    await PromptQuantity(matched[idx]);
                }
            }
            else
            {
                var names = available.Select(i => $"{i.Name} ({i.Unit})").ToArray();
                var selected = await DisplayActionSheet("Select Ingredient", "Cancel", null, names);
                if (string.IsNullOrEmpty(selected) || selected == "Cancel") return;
                var idx = Array.IndexOf(names, selected);
                if (idx < 0) return;
                await PromptQuantity(available[idx]);
            }
        }

        private async Task PromptQuantity(Ingredient ingredient)
        {
            var qtyStr = await DisplayPromptAsync("Quantity",
                $"Enter quantity for {ingredient.Name} ({ingredient.Unit}):",
                accept: "Add",
                placeholder: "0",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyStr)) return;
            if (!decimal.TryParse(qtyStr, out var qty)) return;

            _ingredients.Add(new RecipeIngredient
            {
                IngredientId = ingredient.IngredientId,
                Quantity = qty,
                Ingredient = ingredient
            });
        }

        private void OnRemoveIngredient(object sender, EventArgs e)
        {
            var item = sender as SwipeItem;
            var ri = item?.BindingContext as RecipeIngredient;
            if (ri != null)
                _ingredients.Remove(ri);
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var name = NameEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlert("Validation", "Recipe name is required.", "OK");
                return;
            }

            if (!decimal.TryParse(PriceEntry?.Text?.Trim(), out var price) || price <= 0)
            {
                await DisplayAlert("Validation", "Enter a valid selling price.", "OK");
                return;
            }

            var recipe = _existing ?? new Recipe();
            recipe.Name = name;
            recipe.SellingPrice = price;
            recipe.Description = DescriptionEditor?.Text?.Trim();
            recipe.IsActive = ActiveSwitch.IsToggled;
            recipe.RecipeIngredients = [.. _ingredients];

            _tcs.TrySetResult(recipe);
            await Navigation.PopModalAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            _tcs.TrySetResult(null);
            await Navigation.PopModalAsync();
        }
    }
}
