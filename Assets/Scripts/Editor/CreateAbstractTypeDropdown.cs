using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class CreateAbstractTypeDropdown : AdvancedDropdown
{
    private readonly Type baseType;
    private readonly Action<Type> onSelectType;
    private readonly List<Type> availableTypes;

    public CreateAbstractTypeDropdown(Type baseType, Action<Type> onSelectType)
    : base(new AdvancedDropdownState())
    {
        this.baseType = ExtractElementType(baseType);
        this.onSelectType = onSelectType;
        this.minimumSize = new Vector2(250, 300);
        this.availableTypes = GetSubclasses(this.baseType).ToList();
    }

    private IEnumerable<Type> GetSubclasses(Type type)
    {
        bool isInterface = type.IsInterface;

        var types = Assembly.GetAssembly(type).GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                        (type.IsAssignableFrom(t)));

        return types;
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem("Select Type");

        root.AddChild(new TypeDropdownItem(null) { name = "None" });

        // Group types by category (or null if none)
        var grouped = availableTypes.GroupBy(type =>
        {
            var attr = type.GetCustomAttribute<CategoryAttribute>();
            return attr != null ? attr.Name : null;
        });

        bool hasCategories = grouped.Any(g => g.Key != null);

        if (hasCategories)
        {
            // Create groups for each category
            foreach (var group in grouped)
            {
                if (group.Key == null)
                {
                    // No category add directly to root
                    foreach (var type in group)
                    {
                        root.AddChild(new TypeDropdownItem(type));
                    }
                }
                else
                {
                    var categoryNode = new AdvancedDropdownItem(group.Key);
                    foreach (var type in group)
                    {
                        categoryNode.AddChild(new TypeDropdownItem(type));
                    }
                    root.AddChild(categoryNode);
                }
            }
        }
        else
        {
            // No categories at all flatten list
            foreach (var type in availableTypes.OrderBy(t => t.Name))
            {
                root.AddChild(new TypeDropdownItem(type));
            }
        }

        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        if (item is TypeDropdownItem typedItem)
        {
            onSelectType?.Invoke(typedItem.Type);
        }
    }

    private static Type ExtractElementType(Type type)
    {
        // Handle arrays
        if (type.IsArray)
            return type.GetElementType();

        // Handle List<T>, ICollection<T>, etc.
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IList<>))
            {
                return type.GetGenericArguments()[0];
            }

            // For ManagedReference list elements from SerializedProperty
            if (type == typeof(object) && type.FullName.Contains("ManagedReference"))
            {
                // Try extracting from field metadata (in future tooling)
            }
        }

        return type;
    }


    private class TypeDropdownItem : AdvancedDropdownItem
    {
        public Type Type { get; }

        public TypeDropdownItem(Type type) : base(type?.Name ?? "None")
        {
            Type = type;
        }
    }

    // Static call to show
    public static void Show(Rect position, Type baseType, Action<Type> onSelect)
    {
        var dropdown = new CreateAbstractTypeDropdown(baseType, onSelect);
        dropdown.Show(position);
    }
}
