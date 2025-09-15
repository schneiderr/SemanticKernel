﻿using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AICore.Plugins
{
    public sealed class MenuPlugin
    {
        [KernelFunction, Description("Provides a list of specials from the menu.")]
        public string GetSpecials() =>
            """
        Special Soup: Clam Chowder
        Special Salad: Cobb Salad
        Special Drink: Chai Tea
        """;

        [KernelFunction, Description("Provides the price of the requested menu item.")]
        public string GetItemPrice(
            [Description("The name of the menu item.")]
        string menuItem) =>
            "$9.99";
    }
}
