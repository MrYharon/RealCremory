using System.Collections.ObjectModel;
using Cremory.App.Models;
using Cremory.App.Services;

namespace Cremory.App
{
    public partial class RecipePage : ContentPage
    {
        private readonly ApiService _api;
        private readonly ObservableCollection<Recipe> _recipes = [];
        private List<Recipe> _allRecipes = [];

        public RecipePage(ApiService api)
        {
            InitializeComponent();
            _api = api;
            RecipesCollectionView.ItemsSource = _recipes;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRecipesAsync();
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadRecipesAsync();
            RecipeRefreshView.IsRefreshing = false;
        }

        private async Task LoadRecipesAsync()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            try
            {
                _allRecipes = await _api.GetRecipesAsync();
                _recipes.Clear();
                foreach (var r in _allRecipes)
                    _recipes.Add(r);
            }
            catch
            {
                await DisplayAlert("Error", "Failed to load recipes. Check connection.", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async Task<List<Ingredient>> LoadIngredients()
        {
            var ingredients = await _api.GetIngredientsAsync();
            if (ingredients.Count == 0)
                await DisplayAlert("No Ingredients", "Add ingredients first before creating recipes.", "OK");
            return ingredients;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var query = e.NewTextValue?.Trim() ?? "";
            _recipes.Clear();
            foreach (var r in _allRecipes.Where(r =>
                string.IsNullOrWhiteSpace(query) ||
                r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (r.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)))
                _recipes.Add(r);
        }

        private async void OnAddClicked(object sender, EventArgs e)
        {
            var ingredients = await LoadIngredients();
            if (ingredients.Count == 0) return;

            var formPage = new RecipeFormPage(ingredients);
            await Navigation.PushModalAsync(new NavigationPage(formPage));
            var result = await formPage.Result;

            if (result != null)
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                try
                {
                    var (created, error) = await _api.CreateRecipeAsync(result);
                    if (created)
                    {
                        await DisplayAlert("Success", $"Recipe \"{result.Name}\" created.", "OK");
                        await LoadRecipesAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", error ?? "Failed to create recipe.", "OK");
                    }
                }
                finally
                {
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                }
            }
        }

        private async void OnEditSwipe(object sender, EventArgs e)
        {
            var item = sender as SwipeItem;
            var recipe = item?.BindingContext as Recipe;
            if (recipe == null) return;

            var ingredients = await LoadIngredients();
            if (ingredients.Count == 0) return;

            var formPage = new RecipeFormPage(ingredients, recipe);
            await Navigation.PushModalAsync(new NavigationPage(formPage));
            var result = await formPage.Result;

            if (result != null)
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                try
                {
                    result.RecipeId = recipe.RecipeId;
                    var (updated, updateErr) = await _api.UpdateRecipeAsync(result);
                    if (updated)
                    {
                        await DisplayAlert("Success", "Recipe updated.", "OK");
                        await LoadRecipesAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", updateErr ?? "Failed to update recipe.", "OK");
                    }
                }
                finally
                {
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                }
            }
        }

        private async void OnDeleteSwipe(object sender, EventArgs e)
        {
            var item = sender as SwipeItem;
            var recipe = item?.BindingContext as Recipe;
            if (recipe == null) return;

            var confirm = await DisplayAlert("Delete Recipe",
                $"Delete {recipe.Name}?", "Delete", "Cancel");
            if (!confirm) return;

            var deleted = await _api.DeleteRecipeAsync(recipe.RecipeId);
            if (deleted)
            {
                await LoadRecipesAsync();
                await DisplayAlert("Deleted", $"Recipe \"{recipe.Name}\" deleted.", "OK");
            }
            else
                await DisplayAlert("Error", "Failed to delete recipe.", "OK");
        }
    }
}
