using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Barotrauma;
using MoonSharp.Interpreter;
using Barotrauma.Items.Components;
using MoonSharp.Interpreter.Serialization.Json;
using System;
using System.Text.RegularExpressions;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcLuaComponent : DfcLua2Component
    {
        public DfcLuaComponent(Item item, ContentXElement element) : base(item, element)
        {
        }
    }
}
