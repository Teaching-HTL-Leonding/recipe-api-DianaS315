
using System.Collections.Concurrent;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

/*
    API Routes:
        GET /recipes -> list of all recipes
        POST /recipe -> Add new recipe
        DELETE /recipes/id -> delete recipe by íd
        GET /recipes/filter/keyword -> get list of recipes which contain the keyword in either title or description
        GET /recipes/ingredients/keyword -> get a list of recipes which contain the given ingredient
        PATCH /recipes/id -> replaces a recipe based on id

    Structure of recipe:
        - Title: String
        - Ingredients: List<String>, optional
        - Description: String
        - ImageLink: link to image url as String, optional

*/
var nextRecipeId = 0;
var recipeDict = new ConcurrentDictionary<int, Recipe> { };
var filteredRecipes = new ConcurrentDictionary<int, Recipe> { };


// GETs
app.MapGet("/", () => recipeDict.Values);
app.MapGet("/recipes", () => recipeDict.Values);
app.MapGet("/recipes/{id}", (int id) =>
{
    if (recipeDict.TryGetValue(id, out Recipe? recipe)) { return Results.Ok(recipe); }
    return Results.NotFound();
});
app.MapGet("/recipes/filter/{keyword}", (string keyword) =>
{
    for (int i = 1; i < recipeDict.Count()+1; i++)
    {
        if (recipeDict[i].Title.ToLower().Contains(keyword.ToLower()) || recipeDict[i].Description.ToLower().Contains(keyword.ToLower()))
        {
            filteredRecipes.TryAdd(i, recipeDict[i]);
        }
    }
    return Results.Ok(filteredRecipes);
});

app.MapGet("/recipes/ingredients/{keyword}", (string keyword) =>
{
    for (int i = 1; i < recipeDict.Count()+1; i++)
    {
        if (recipeDict[i].Ingredients != null)
        {
            if (recipeDict[i].Ingredients!.Contains(keyword))
            {
                filteredRecipes.TryAdd(i, recipeDict[i]);
            }
        }
    }
    return Results.Ok(filteredRecipes);
});

// POSTs

app.MapPost("/recipes", (CreateRecipeDto newRecipe) =>
{

    var newId = Interlocked.Increment(ref nextRecipeId);

    var recipeToAdd = new Recipe
    {
        Id = newId,
        Title = newRecipe.Title,
        Ingredients = newRecipe.Ingredients,
        Description = newRecipe.Description,
        ImageLink = newRecipe.ImageLink
    };

    if (!recipeDict.TryAdd(newId, recipeToAdd)) { return Results.StatusCode(StatusCodes.Status500InternalServerError); }
    return Results.Created($"/recipes/{newId}", recipeToAdd);
});

//DELETEs

app.MapDelete("/recipes/{id}", (int id) =>
{
    if (recipeDict.ContainsKey(id))
    {
        if (recipeDict.TryRemove(id, out Recipe? recipe)) { return Results.NoContent(); }
    }
    return Results.NotFound($"Recipe with id {id} could not be found");
});

//PATCHes

app.MapPatch("/recipes/{id}", (int id, CreateRecipeDto newRecipe) =>
{

    var recipeToAdd = new Recipe
    {
        Id = id,
        Title = newRecipe.Title,
        Ingredients = newRecipe.Ingredients,
        Description = newRecipe.Description,
        ImageLink = newRecipe.ImageLink
    };
    if (recipeDict.ContainsKey(id))
    {
        if (!recipeDict.TryUpdate(id, recipeToAdd, recipeDict[id])) { return Results.StatusCode(StatusCodes.Status500InternalServerError); }
        return Results.Created($"/recipes/{id}", recipeToAdd);
    }
    return Results.NotFound($"Recipe with id {id} could not be found");
});

app.Run();

class Recipe
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public List<String>? Ingredients { get; set; }
    public string Description { get; set; } = "";
    public string? ImageLink { get; set; }
}


record CreateRecipeDto(string Title, List<String>? Ingredients, string Description, string? ImageLink);