using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public string Name;
    public string Description;

    public Materials Material;
    public int Value;

    public Item(ClassificationResult classification)
    {
        Name = classification.ClassName;
        Description = GetItemDescription(Name);
        Material = classification.Material;
        Value = Mathf.RoundToInt(GetBaseItemValue(Name) * (1f + classification.Confidence));
    }

    // Value dictionary
    public static readonly Dictionary<string, float> ItemValues = new Dictionary<string, float>()
    {
        {"Aluminium foil", 8f},
        {"Battery", 50f},
        {"Aluminium blister pack", 5f},
        {"Carded blister pack", 3f},
        {"Other plastic bottle", 8f},
        {"Clear plastic bottle", 10f},
        {"Glass bottle", 12f},
        {"Plastic bottle cap", 2f},
        {"Metal bottle cap", 5f},
        {"Broken glass", 1f},
        {"Food Can", 12f},
        {"Aerosol", 10f},
        {"Drink can", 10f},
        {"Toilet tube", 1f},
        {"Other carton", 3f},
        {"Egg carton", 4f},
        {"Drink carton", 5f},
        {"Corrugated carton", 6f},
        {"Meal carton", 6f},
        {"Pizza box", 7f},
        {"Paper cup", 3f},
        {"Disposable plastic cup", 2f},
        {"Foam cup", 1f},
        {"Glass cup", 8f},
        {"Other plastic cup", 2f},
        {"Food waste", 1f},
        {"Glass jar", 15f},
        {"Plastic lid", 2f},
        {"Metal lid", 4f},
        {"Other plastic", 1f},
        {"Magazine paper", 3f},
        {"Tissues", 1f},
        {"Wrapping paper", 2f},
        {"Normal paper", 3f},
        {"Paper bag", 3f},
        {"Plastified paper bag", 2f},
        {"Plastic film", 1f},
        {"Six pack rings", 2f},
        {"Garbage bag", 1f},
        {"Other plastic wrapper", 1f},
        {"Single-use carrier bag", 2f},
        {"Polypropylene bag", 2f},
        {"Crisp packet", 1f},
        {"Spread tub", 5f},
        {"Tupperware", 6f},
        {"Disposable food container", 3f},
        {"Foam food container", 1f},
        {"Other plastic container", 3f},
        {"Plastic glooves", 1f},
        {"Plastic utensils", 1f},
        {"Pop tab", 2f},
        {"Rope & strings", 2f},
        {"Scrap metal", 20f},
        {"Shoe", 5f},
        {"Squeezable tube", 2f},
        {"Plastic straw", 1f},
        {"Paper straw", 1f},
        {"Styrofoam piece", 1f},
        {"Unlabeled litter", 1f},
        {"Cigarette", 1f},
    };

    // Description dictionary
    public static readonly Dictionary<string, string> ItemDescriptions = new Dictionary<string, string>()
    {
        {"Aluminium foil", "Thin sheet of aluminum used for wrapping food."},
        {"Battery", "Disposable or rechargeable energy source."},
        {"Aluminium blister pack", "Blister pack made of aluminum, often for pills."},
        {"Carded blister pack", "Blister pack attached to a cardboard backing."},
        {"Other plastic bottle", "Plastic bottle not specifically categorized."},
        {"Clear plastic bottle", "Transparent plastic bottle for drinks or liquids."},
        {"Glass bottle", "Reusable or disposable glass bottle."},
        {"Plastic bottle cap", "Cap made of plastic, typically for bottles."},
        {"Metal bottle cap", "Metal cap from a bottle, recyclable."},
        {"Broken glass", "Shattered glass pieces, dangerous waste."},
        {"Food Can", "Metal can used for food packaging."},
        {"Aerosol", "Pressurized can containing spray products."},
        {"Drink can", "Aluminum can for beverages."},
        {"Toilet tube", "Empty cardboard tube from toilet paper."},
        {"Other carton", "Generic carton packaging."},
        {"Egg carton", "Carton for eggs, usually cardboard."},
        {"Drink carton", "Carton for milk or juice."},
        {"Corrugated carton", "Box made from corrugated cardboard."},
        {"Meal carton", "Packaging for ready meals."},
        {"Pizza box", "Cardboard box for pizza."},
        {"Paper cup", "Single-use paper cup."},
        {"Disposable plastic cup", "Plastic cup for one-time use."},
        {"Foam cup", "Styrofoam cup for beverages."},
        {"Glass cup", "Reusable or disposable glass cup."},
        {"Other plastic cup", "Plastic cup not specifically categorized."},
        {"Food waste", "Organic waste from food scraps."},
        {"Glass jar", "Jar made of glass for food or storage."},
        {"Plastic lid", "Plastic lid from containers."},
        {"Metal lid", "Metal lid from jars or cans."},
        {"Other plastic", "Plastic item not otherwise categorized."},
        {"Magazine paper", "Printed magazine pages."},
        {"Tissues", "Disposable tissue paper."},
        {"Wrapping paper", "Paper used for wrapping gifts or items."},
        {"Normal paper", "Plain paper like notebooks or printing sheets."},
        {"Paper bag", "Paper bag used for shopping or packaging."},
        {"Plastified paper bag", "Paper bag coated with plastic for durability."},
        {"Plastic film", "Thin sheet of plastic, e.g., packaging wrap."},
        {"Six pack rings", "Plastic rings used to hold beverage cans together."},
        {"Garbage bag", "Plastic bag used for trash collection."},
        {"Other plastic wrapper", "Plastic wrapper not categorized elsewhere."},
        {"Single-use carrier bag", "Disposable shopping bag."},
        {"Polypropylene bag", "Plastic bag made from polypropylene."},
        {"Crisp packet", "Snack packaging, usually plastic/aluminum mix."},
        {"Spread tub", "Plastic tub for spreads like butter or margarine."},
        {"Tupperware", "Reusable plastic container for food storage."},
        {"Disposable food container", "Plastic container for takeout food."},
        {"Foam food container", "Styrofoam container for takeout."},
        {"Other plastic container", "Plastic container not otherwise categorized."},
        {"Plastic glooves", "Single-use plastic gloves."},
        {"Plastic utensils", "Disposable plastic cutlery."},
        {"Pop tab", "Metal pull tab from cans."},
        {"Rope & strings", "Pieces of rope or string."},
        {"Scrap metal", "Metal waste from various sources."},
        {"Shoe", "Footwear item."},
        {"Squeezable tube", "Plastic tube containing toothpaste, cream, or sauce."},
        {"Plastic straw", "Single-use plastic straw."},
        {"Paper straw", "Paper straw alternative to plastic."},
        {"Styrofoam piece", "Pieces of Styrofoam, often from packaging."},
        {"Unlabeled litter", "Trash with no clear category."},
        {"Cigarette", "Cigarette butt."},
    };

    // Helper functions
    public static float GetBaseItemValue(string className)
    {
        return ItemValues.TryGetValue(className, out float value) ? value : 1f;
    }

    public static string GetItemDescription(string className)
    {
        return ItemDescriptions.TryGetValue(className, out string desc) ? desc : "No description.";
    }
}