using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class CategoryAttribute : Attribute
{
    public string Name { get; }

    public CategoryAttribute(
        string name)
    {
        Name = name;
    }
}