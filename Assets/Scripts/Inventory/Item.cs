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
        {"Aluminium foil", 80f},
        {"Battery", 400f},
        {"Aluminium blister pack", 50f},
        {"Carded blister pack", 30f},
        {"Other plastic bottle", 80f},
        {"Clear plastic bottle", 100f},
        {"Glass bottle", 120f},
        {"Plastic bottle cap", 20f},
        {"Metal bottle cap", 50f},
        {"Broken glass", 10f},
        {"Food Can", 120f},
        {"Aerosol", 100f},
        {"Drink can", 100f},
        {"Toilet tube", 10f},
        {"Other carton", 30f},
        {"Egg carton", 40f},
        {"Drink carton", 50f},
        {"Corrugated carton", 60f},
        {"Meal carton", 60f},
        {"Pizza box", 70f},
        {"Paper cup", 30f},
        {"Disposable plastic cup", 20f},
        {"Foam cup", 10f},
        {"Glass cup", 80f},
        {"Other plastic cup", 20f},
        {"Food waste", 10f},
        {"Glass jar", 150f},
        {"Plastic lid", 20f},
        {"Metal lid", 40f},
        {"Other plastic", 10f},
        {"Magazine paper", 30f},
        {"Tissues", 10f},
        {"Wrapping paper", 20f},
        {"Normal paper", 30f},
        {"Paper bag", 30f},
        {"Plastified paper bag", 20f},
        {"Plastic film", 10f},
        {"Six pack rings", 20f},
        {"Garbage bag", 10f},
        {"Other plastic wrapper", 10f},
        {"Single-use carrier bag", 20f},
        {"Polypropylene bag", 20f},
        {"Crisp packet", 10f},
        {"Spread tub", 50f},
        {"Tupperware", 60f},
        {"Disposable food container", 30f},
        {"Foam food container", 10f},
        {"Other plastic container", 30f},
        {"Plastic glooves", 10f},
        {"Plastic utensils", 10f},
        {"Pop tab", 20f},
        {"Rope & strings", 20f},
        {"Scrap metal", 200f},
        {"Shoe", 50f},
        {"Squeezable tube", 20f},
        {"Plastic straw", 10f},
        {"Paper straw", 10f},
        {"Styrofoam piece", 10f},
        {"Unlabeled litter", 10f},
        {"Cigarette", 10f},
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